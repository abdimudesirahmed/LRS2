using System.ComponentModel.DataAnnotations;
using LRS.Domain.Common;

namespace LRS.Domain.Entities;

public partial class AdministrativeSource : IAggregateRoot
{
    [Key]
    public int Id { get; set; }
    public int SourceId { get; set; }
    public int AdministrativeSourceTypeId { get; set; }
    public int? ApplicationId { get; set; }
    public int? BaUnitId { get; set; }
    
    // Navigation property
    public virtual AdministrativeSourceType AdministrativeSourceType { get; set; } = null!;
}
