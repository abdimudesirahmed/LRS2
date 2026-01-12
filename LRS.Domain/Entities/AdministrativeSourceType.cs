using System.ComponentModel.DataAnnotations;
using LRS.Domain.Common;

namespace LRS.Domain.Entities;

public partial class AdministrativeSourceType : IAggregateRoot
{
    [Key]
    public int Id { get; set; }
    public string? AmharicValue { get; set; }
    public required string EnglishValue { get; set; }
    public string? OromifaValue { get; set; }
    public string? TigrinyaValue { get; set; }
    public string? HarariValue { get; set; }
}
