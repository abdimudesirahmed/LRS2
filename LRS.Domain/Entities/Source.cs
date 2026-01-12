using System.ComponentModel.DataAnnotations;
using LRS.Domain.Common;

namespace LRS.Domain.Entities;

public partial class Source : IAggregateRoot
{
    [Key]
    public int Id { get; set; }
    public DateTime? RecordationDate { get; set; }
    public bool Status { get; set; } = false;
    public bool IsCreated { get; set; }
    public virtual AdministrativeSource? AdministrativeSource { get; set; }
}
