using System.ComponentModel.DataAnnotations;
using LRS.Domain.Common;

namespace LRS.Domain.Entities;

public partial class Document : IAggregateRoot
{
    [Key]
    public int Id { get; set; }
    public int? SourceId { get; set; }
    public string? DocumentName { get; set; } // AdministrativeSourceType EnglishValue
    public DateTime? SubmissionDate { get; set; } // current date from the system
    public string? AlfDocumentId { get; set; } // node id
    public required string AppRegId { get; set; } // application registration id from user input
    public string? UniqueParcelId { get; set; } // UniqueParcelId from user input (optional/deprecated)
    public bool IsVoid { get; set; } = false;
    public string? ModifiedBy { get; set; }
    public string? CreatedBy { get; set; }
    
    public virtual Source? Source { get; set; }
}
