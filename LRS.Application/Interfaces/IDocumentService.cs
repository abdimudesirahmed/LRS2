using LRS.Application.DTOs;

namespace LRS.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponseDto> UploadDocumentAsync(UploadDocumentDto dto);
    Task<(Stream FileStream, string ContentType, string FileName)> GetDocumentFileAsync(int documentId);
    Task<IEnumerable<DocumentResponseDto>> GetDocumentsBySourceIdAsync(int sourceId);
    Task<DocumentResponseDto?> GetLatestByParcelAndTypeAsync(string parcelId, int adminSourceTypeId);
}
