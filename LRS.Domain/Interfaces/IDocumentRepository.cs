using LRS.Domain.Entities;

namespace LRS.Domain.Interfaces;

public interface IDocumentRepository : IGenericRepository<Document>
{
    Task<Document?> GetLatestBySourceAndTypeAsync(int sourceId, int adminSourceTypeId);
}

