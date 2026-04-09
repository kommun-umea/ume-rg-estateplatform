using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers;

public static class PythagorasFileDocumentMapper
{
    public static DocumentInfoModel ToModel(FileDocument dto, int? directoryId)
    {
        return new DocumentInfoModel
        {
            Id = dto.Id,
            Name = dto.Name,
            DirectoryId = directoryId,
            SizeInBytes = dto.DataSize,
            CategoryId = dto.ActionTypeId,
            CategoryName = dto.ActionTypeName,
        };
    }

    public static List<DocumentInfoModel> ToModelWithDirectoryId(IReadOnlyList<FileDocument> documentDtos, int? directoryId)
    {
        ArgumentNullException.ThrowIfNull(documentDtos);

        return documentDtos.Count == 0
            ? []
            : documentDtos.Select(d => ToModel(d, directoryId)).ToList();
    }
}
