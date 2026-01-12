using System.ComponentModel.DataAnnotations;

namespace LRS.Domain.Common;

public interface IAggregateRoot
{
    // [Key] is not valid on interface members usually, but following the spec
    int Id { get; set; }
}
