using LRS.Domain.Entities;
using LRS.Domain.Interfaces;
using LRS.Infrastructure.Persistence;

namespace LRS.Infrastructure.Repositories;

public class SourceRepository : GenericRepository<Source>, ISourceRepository
{
    public SourceRepository(LrsDbContext context) : base(context)
    {
    }
}
