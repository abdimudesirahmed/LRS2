using LRS.Domain.Entities;
using LRS.Domain.Interfaces;
using LRS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LRS.Infrastructure.Repositories;

public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
{
    public DocumentRepository(LrsDbContext context) : base(context)
    {
    }

    public async Task<Document?> GetLatestBySourceAndTypeAsync(int sourceId, int adminSourceTypeId)
    {
        return await _context.Documents
            .Include(d => d.Source)
                .ThenInclude(s => s!.AdministrativeSource)
                    .ThenInclude(a => a!.AdministrativeSourceType)
            .Where(d => d.SourceId == sourceId && d.Source != null && d.Source.AdministrativeSource != null && d.Source.AdministrativeSource.AdministrativeSourceTypeId == adminSourceTypeId)
            .OrderByDescending(d => d.SubmissionDate)
            .FirstOrDefaultAsync();
    }

    public async Task<Document?> GetLatestByParcelAndTypeAsync(string parcelId, int adminSourceTypeId)
    {
        return await _context.Documents
            .Include(d => d.Source)
                .ThenInclude(s => s!.AdministrativeSource)
            .Where(d => d.UniqueParcelId == parcelId && d.Source != null && d.Source.AdministrativeSource != null && d.Source.AdministrativeSource.AdministrativeSourceTypeId == adminSourceTypeId)
            .OrderByDescending(d => d.SubmissionDate)
            .FirstOrDefaultAsync();
    }
}
