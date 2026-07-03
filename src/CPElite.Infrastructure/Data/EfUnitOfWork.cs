using CPElite.Application.Abstractions;

namespace CPElite.Infrastructure.Data;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly CPEliteDbContext _dbContext;

    public EfUnitOfWork(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
