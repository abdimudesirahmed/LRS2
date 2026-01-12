using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LRS.Application.DTOs;

/// <summary>
/// DTO for uploading documents (SRS User Story 1: Scan and Upload Document)
/// </summary>
public class UploadDocumentDto
{
    // If SourceId is provided, we link to existing source
    public int? SourceId { get; set; }
    
    [Required(ErrorMessage = "Administrative Source Type is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Administrative Source Type must be a valid ID")]
    public int AdministrativeSourceTypeId { get; set; }
    
    // Requirements: AppRegId and UniqueParcelId from user input (SRS: Metadata form is mandatory)
    [Required(ErrorMessage = "Application Registration ID is required")]
    [StringLength(100, ErrorMessage = "Application Registration ID cannot exceed 100 characters")]
    public required string AppRegId { get; set; }
    
    // UniqueParcelId removed as per requirements (handled by Alfresco/System)
    // public string? UniqueParcelId { get; set; }
    
    // File to upload (SRS: User can scan or upload PDF/image)
    [Required(ErrorMessage = "File is required")]
    public required IFormFile File { get; set; }
    
    [StringLength(100, ErrorMessage = "Created By cannot exceed 100 characters")]
    public string? CreatedBy { get; set; }
    
    // Additional metadata if needed for new Source creation
    public DateTime? RecordationDate { get; set; }
}
