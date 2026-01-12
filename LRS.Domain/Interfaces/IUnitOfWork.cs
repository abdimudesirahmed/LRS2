using LRS.Domain.Entities;

namespace LRS.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ISourceRepository Sources { get; }
    IDocumentRepository Documents { get; }
    IGenericRepository<AdministrativeSource> AdministrativeSources { get; }
    IGenericRepository<AdministrativeSourceType> AdministrativeSourceTypes { get; }

    Task<int> SaveChangesAsync();
}
