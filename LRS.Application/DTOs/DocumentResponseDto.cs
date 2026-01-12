namespace LRS.Application.DTOs;

public class DocumentResponseDto
{
    public int Id { get; set; }
    public int? SourceId { get; set; }
    public string? DocumentName { get; set; }
    public DateTime? SubmissionDate { get; set; }
    public string? AlfDocumentId { get; set; }
    public required string AppRegId { get; set; }
    public string? UniqueParcelId { get; set; }
    public bool IsVoid { get; set; }
    public string? AdminSourceTypeEnglish { get; set; }
}
