using Microsoft.EntityFrameworkCore;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.DataStore.Entities;
using Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public class FileDocumentHandler(
    IPythagorasClient pythagorasClient,
    IDataStore dataStore,
    IDbContextFactory<EstateDbContext> dbContextFactory) : IFileDocumentHandler
{
    private const int MaxParallelInfoRequests = 10;

    public async Task<DocumentFileModel?> GetBuildingDocument(int buildingId, int directoryId, int documentId, CancellationToken cancellationToken = default)
    {
        if (!await DirectoryBelongsToBuilding(buildingId, directoryId, cancellationToken))
        {
            return null;
        }

        FileDocument? documentDto = await GetDocumentInDirectory(directoryId, documentId, cancellationToken);
        if (documentDto is null)
        {
            return null;
        }

        FileDocumentInfo? info = await pythagorasClient.GetDocumentInfoAsync(documentId, cancellationToken);
        if (info is null || !info.RecordStatusId.HasValue || !dataStore.PortalPublishStatusIds.Contains(info.RecordStatusId.Value))
        {
            return null;
        }

        (byte[] data, string contentType) = await pythagorasClient.GetDocument(documentId, cancellationToken);

        return new DocumentFileModel
        {
            Name = documentDto.Name,
            ContentType = contentType,
            Content = data
        };
    }
    private async Task<bool> DirectoryBelongsToBuilding(int buildingId, int directoryId, CancellationToken cancellationToken)
    {
        FileDocumentDirectory? directory = await pythagorasClient.GetDirectory(directoryId, cancellationToken);

        return directory is not null
            && string.Equals(directory.EntityType, "building", StringComparison.OrdinalIgnoreCase)
            && directory.EntityId == buildingId;
    }

    private async Task<FileDocument?> GetDocumentInDirectory(int directoryId, int documentId, CancellationToken cancellationToken)
    {
        PythagorasQuery<FileDocument> query = new PythagorasQuery<FileDocument>()
            .WithQueryParameter("includeActionType", "true");

        IReadOnlyList<FileDocument> documents = await pythagorasClient.GetDirectoryDocuments(directoryId, query, cancellationToken);
        return documents.FirstOrDefault(d => d.Id == documentId);
    }

    public async Task<BuildingDocumentTreeModel> GetBuildingDocumentTree(int buildingId, CancellationToken cancellationToken = default)
    {
        List<DocumentDirectoryInfoModel> allDirectories = [];
        List<DocumentInfoModel> allDocuments = [];

        // Get root level items
        IReadOnlyList<FileDocument> rootDocuments = await pythagorasClient.GetBuildingRootDocuments(buildingId, null, cancellationToken);
        IReadOnlyList<FileDocumentDirectory> rootDirectories = await pythagorasClient.GetBuildingRootDirectories(buildingId, null, cancellationToken);

        // Add root documents (no directory ID since they're at root level)
        List<DocumentInfoModel> rootDocModels = PythagorasFileDocumentMapper.ToModelWithDirectoryId(rootDocuments, null);
        List<DocumentInfoModel> filteredRootDocs = await FilterByPortalStatusAsync(rootDocModels, cancellationToken);
        allDocuments.AddRange(filteredRootDocs);

        // Process each root directory recursively
        foreach (FileDocumentDirectory rootDir in rootDirectories)
        {
            await CollectDirectoryContentsRecursively(rootDir, allDirectories, allDocuments, cancellationToken);
        }

        return new BuildingDocumentTreeModel
        {
            TotalDocumentCount = allDocuments.Count,
            TotalDirectoryCount = allDirectories.Count,
            Directories = allDirectories,
            Documents = allDocuments
        };
    }

    private async Task CollectDirectoryContentsRecursively(
        FileDocumentDirectory directory,
        List<DocumentDirectoryInfoModel> allDirectories,
        List<DocumentInfoModel> allDocuments,
        CancellationToken cancellationToken)
    {
        // Add this directory
        allDirectories.Add(new DocumentDirectoryInfoModel
        {
            Id = directory.Id,
            Name = directory.Name,
            ParentId = directory.ParentId,
            DocumentCount = directory.NumberOfChildFiles,
            DirectoryCount = directory.NumberOfChildFolders
        });

        // Get documents in this directory
        PythagorasQuery<FileDocument> docQuery = new PythagorasQuery<FileDocument>()
            .WithQueryParameter("includeActionType", "true");
        IReadOnlyList<FileDocument> documents = await pythagorasClient.GetDirectoryDocuments(directory.Id, docQuery, cancellationToken);
        List<DocumentInfoModel> docModels = PythagorasFileDocumentMapper.ToModelWithDirectoryId(documents, directory.Id);
        List<DocumentInfoModel> filteredDocs = await FilterByPortalStatusAsync(docModels, cancellationToken);
        allDocuments.AddRange(filteredDocs);

        // Get and process child directories
        IReadOnlyList<FileDocumentDirectory> childDirectories = await pythagorasClient.GetChildDirectories(directory.Id, null, cancellationToken);
        foreach (FileDocumentDirectory childDir in childDirectories)
        {
            await CollectDirectoryContentsRecursively(childDir, allDirectories, allDocuments, cancellationToken);
        }
    }

    public async Task<BuildingDocumentTreeNestedModel> GetBuildingDocumentTreeNested(int buildingId, CancellationToken cancellationToken = default)
    {
        int totalDocuments = 0;
        int totalDirectories = 0;

        // Get root level items
        IReadOnlyList<FileDocument> rootDocuments = await pythagorasClient.GetBuildingRootDocuments(buildingId, null, cancellationToken);
        IReadOnlyList<FileDocumentDirectory> rootDirectories = await pythagorasClient.GetBuildingRootDirectories(buildingId, null, cancellationToken);

        List<DocumentInfoModel> rootDocModels = PythagorasFileDocumentMapper.ToModelWithDirectoryId(rootDocuments, null);
        List<DocumentInfoModel> filteredRootDocs = await FilterByPortalStatusAsync(rootDocModels, cancellationToken);
        totalDocuments += filteredRootDocs.Count;

        // Build nested tree for each root directory
        List<DocumentTreeNodeModel> nestedDirectories = [];
        foreach (FileDocumentDirectory rootDir in rootDirectories)
        {
            DocumentTreeNodeModel node = await BuildDirectoryTreeRecursively(rootDir, cancellationToken);
            nestedDirectories.Add(node);
            CountItems(node, ref totalDocuments, ref totalDirectories);
        }

        return new BuildingDocumentTreeNestedModel
        {
            TotalDocumentCount = totalDocuments,
            TotalDirectoryCount = totalDirectories,
            Directories = nestedDirectories,
            RootDocuments = filteredRootDocs
        };
    }

    private async Task<DocumentTreeNodeModel> BuildDirectoryTreeRecursively(
        FileDocumentDirectory directory,
        CancellationToken cancellationToken)
    {
        // Get documents in this directory
        PythagorasQuery<FileDocument> docQuery = new PythagorasQuery<FileDocument>()
            .WithQueryParameter("includeActionType", "true");
        IReadOnlyList<FileDocument> documents = await pythagorasClient.GetDirectoryDocuments(directory.Id, docQuery, cancellationToken);
        List<DocumentInfoModel> docModels = PythagorasFileDocumentMapper.ToModelWithDirectoryId(documents, directory.Id);
        List<DocumentInfoModel> filteredDocs = await FilterByPortalStatusAsync(docModels, cancellationToken);

        // Get and process child directories
        IReadOnlyList<FileDocumentDirectory> childDirectories = await pythagorasClient.GetChildDirectories(directory.Id, null, cancellationToken);

        List<DocumentTreeNodeModel> childNodes = [];
        foreach (FileDocumentDirectory childDir in childDirectories)
        {
            childNodes.Add(await BuildDirectoryTreeRecursively(childDir, cancellationToken));
        }

        return new DocumentTreeNodeModel
        {
            Id = directory.Id,
            Name = directory.Name,
            Subdirectories = childNodes,
            Documents = filteredDocs
        };
    }

    private static void CountItems(DocumentTreeNodeModel node, ref int totalDocuments, ref int totalDirectories)
    {
        totalDirectories++;
        totalDocuments += node.Documents.Count;

        foreach (DocumentTreeNodeModel child in node.Subdirectories)
        {
            CountItems(child, ref totalDocuments, ref totalDirectories);
        }
    }

    public async Task<int> GetBuildingDocumentCountAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        await using EstateDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.BuildingDocuments
            .Where(r => r.BuildingId == buildingId)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentInfoModel>> GetBuildingDocumentsForPortalAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        await using EstateDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<BuildingDocumentEntity> rows = await context.BuildingDocuments
            .AsNoTracking()
            .Where(r => r.BuildingId == buildingId)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new DocumentInfoModel
        {
            Id = r.DocumentId,
            Name = r.Name,
            SizeInBytes = r.SizeInBytes,
            CategoryId = r.CategoryId,
            CategoryName = r.CategoryName,
        }).ToList();
    }

    public async Task<DocumentFileModel?> GetBuildingDocumentForPortalAsync(int buildingId, int documentId, CancellationToken cancellationToken = default)
    {
        FileDocumentInfo? info = await pythagorasClient.GetDocumentInfoAsync(documentId, cancellationToken);

        if (info is null
            || !string.Equals(info.EntityType, "building", StringComparison.OrdinalIgnoreCase)
            || info.EntityId != buildingId
            || !info.RecordStatusId.HasValue
            || !dataStore.PortalPublishStatusIds.Contains(info.RecordStatusId.Value))
        {
            return null;
        }

        (byte[] data, string contentType) = await pythagorasClient.GetDocument(documentId, cancellationToken);

        return new DocumentFileModel
        {
            Name = info.Name,
            ContentType = contentType,
            Content = data
        };
    }

    private async Task<List<DocumentInfoModel>> FilterByPortalStatusAsync(List<DocumentInfoModel> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return [];
        }

        Dictionary<int, FileDocumentInfo> infoLookup = await GetDocumentInfoLookupAsync(documents, cancellationToken);

        return documents
            .Where(doc => infoLookup.TryGetValue(doc.Id, out FileDocumentInfo? info) && info.RecordStatusId.HasValue && dataStore.PortalPublishStatusIds.Contains(info.RecordStatusId.Value) && info.VersionRank == 1)
            .ToList();
    }

    private async Task<Dictionary<int, FileDocumentInfo>> GetDocumentInfoLookupAsync(List<DocumentInfoModel> documents, CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return [];
        }

        using SemaphoreSlim semaphore = new(MaxParallelInfoRequests);
        Task<FileDocumentInfo?>[] tasks = documents.Select(async doc =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await pythagorasClient.GetDocumentInfoAsync(doc.Id, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        FileDocumentInfo?[] results = await Task.WhenAll(tasks);

        Dictionary<int, FileDocumentInfo> lookup = [];
        foreach (FileDocumentInfo? info in results)
        {
            if (info is not null)
            {
                lookup[info.Id] = info;
            }
        }
        return lookup;
    }
}
