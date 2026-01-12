using LRS.Domain.Interfaces;
using LRS.Infrastructure.Persistence;
using LRS.Domain.Entities;

namespace LRS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LrsDbContext _context;

    public ISourceRepository Sources { get; }
    public IDocumentRepository Documents { get; }
    public IGenericRepository<AdministrativeSource> AdministrativeSources { get; }
    public IGenericRepository<AdministrativeSourceType> AdministrativeSourceTypes { get; }

    public UnitOfWork(LrsDbContext context, ISourceRepository sourceRepository, IDocumentRepository documentRepository)
    {
        _context = context;
        Sources = sourceRepository;
        Documents = documentRepository;
        AdministrativeSources = new GenericRepository<AdministrativeSource>(_context);
        AdministrativeSourceTypes = new GenericRepository<AdministrativeSourceType>(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
