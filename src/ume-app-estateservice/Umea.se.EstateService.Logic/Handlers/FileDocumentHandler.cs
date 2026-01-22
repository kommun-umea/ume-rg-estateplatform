using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public class FileDocumentHandler(IPythagorasClient pythagorasClient) : IFileDocumentHandler
{
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
        allDocuments.AddRange(PythagorasFileDocumentMapper.ToModelWithDirectoryId(rootDocuments, null));

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
        allDocuments.AddRange(PythagorasFileDocumentMapper.ToModelWithDirectoryId(documents, directory.Id));

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

        totalDocuments += rootDocuments.Count;

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
            RootDocuments = PythagorasFileDocumentMapper.ToModelWithDirectoryId(rootDocuments, null)
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
            Documents = PythagorasFileDocumentMapper.ToModelWithDirectoryId(documents, directory.Id)
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
        // Use uilistdata endpoint with maxResults=0 to just get the count
        UiListDataResponse<FileDocument> response = await pythagorasClient.GetBuildingDocumentListAsync(buildingId, maxResults: 0, cancellationToken);
        return response.TotalSize;
    }
}
