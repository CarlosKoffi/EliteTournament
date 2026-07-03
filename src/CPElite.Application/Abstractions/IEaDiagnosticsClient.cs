namespace CPElite.Application.Abstractions;

public interface IEaDiagnosticsClient
{
    Task<EaDiagnosticProbeResult> ProbeAsync(string endpoint, CancellationToken cancellationToken = default);
}

public sealed record EaDiagnosticProbeResult(bool Success, int? StatusCode, string? Error, string? RawBody);
