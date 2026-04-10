using System;
using System.Collections.Generic;

namespace LRS.Domain.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<FidoCredential> FidoCredentials { get; set; } = new List<FidoCredential>();
}
