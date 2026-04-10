using LRS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace LRS.Infrastructure.Persistence;

public class LrsDbContext : DbContext
{
    public LrsDbContext(DbContextOptions<LrsDbContext> options)
        : base(options) { }

    public DbSet<Source> Sources => Set<Source>();
    public DbSet<AdministrativeSource> AdministrativeSources => Set<AdministrativeSource>();
    public DbSet<AdministrativeSourceType> AdministrativeSourceTypes => Set<AdministrativeSourceType>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<FidoCredential> FidoCredentials => Set<FidoCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdministrativeSourceType>().HasData(
            new AdministrativeSourceType { Id = 1, AmharicValue = "የይዞታ ማረጋገጫ ሰርተፊኬት", EnglishValue = "Title Certificate", OromifaValue = "Waraqaa ragaa eenyummaa mirkaneessu" },
            new AdministrativeSourceType { Id = 2, AmharicValue = "የመሬት ግብር ክፍያ ደረሰኝ", EnglishValue = "Tax Receipt", OromifaValue = "Waraqaa Kaffaltii Gibiraa" },
            new AdministrativeSourceType { Id = 3, AmharicValue = "የእግድ ደብዳቤ", EnglishValue = "Injunction registration Letter", OromifaValue = "Xalayaa Dhoorkii Mana Murtii" },
            new AdministrativeSourceType { Id = 4, AmharicValue = "የእግድ ስረዛ ደብዳቤ", EnglishValue = "Injunction release Letter", OromifaValue = "Xalayaa Dhoorkii Mana Murtii Kaasu" },
            new AdministrativeSourceType { Id = 5, AmharicValue = "የዕዳ ምዝገባ ደብዳቤ", EnglishValue = "Mortgage registration letter", OromifaValue = "Xalayaa Dhoorkii Baankii" },
            new AdministrativeSourceType { Id = 6, AmharicValue = "የዕዳ ስረዛ ደብዳቤ", EnglishValue = "Mortgage registration release", OromifaValue = "Xalayaa Dhoorkii Baankii Kaasu" },
            new AdministrativeSourceType { Id = 7, AmharicValue = "የከፊል ሽያጭ ውል ሰነድ", EnglishValue = "Partial Sale Contract", OromifaValue = "Galmee Waliigaltee Walakkaa Gurgurtaa" }
        );
    }
}
