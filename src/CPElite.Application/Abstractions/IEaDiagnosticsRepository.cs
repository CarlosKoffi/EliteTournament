using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface IEaDiagnosticsRepository
{
    Task AddAsync(EaDiagnosticProbe probe, CancellationToken cancellationToken = default);
}
