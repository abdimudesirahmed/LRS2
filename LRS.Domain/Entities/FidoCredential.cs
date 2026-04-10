using System;

namespace LRS.Domain.Entities;

public class FidoCredential
{
    public int Id { get; set; }
    
    // Foreign key to AppUser
    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public byte[] UserHandle { get; set; } = Array.Empty<byte>();
    public uint SignatureCounter { get; set; }
    public int CredType { get; set; }
    public DateTime RegDate { get; set; } = DateTime.UtcNow;
    public Guid AaGuid { get; set; }
}
