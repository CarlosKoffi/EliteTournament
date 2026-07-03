using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;

namespace CPElite.Infrastructure.Repositories;

public sealed class EaDiagnosticsRepository : IEaDiagnosticsRepository
{
    private readonly CPEliteDbContext _dbContext;

    public EaDiagnosticsRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EaDiagnosticProbe probe, CancellationToken cancellationToken = default)
    {
        await _dbContext.EaDiagnosticProbes.AddAsync(probe, cancellationToken);
    }
}
