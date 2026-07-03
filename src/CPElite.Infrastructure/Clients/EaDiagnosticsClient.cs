using CPElite.Application.Abstractions;

namespace CPElite.Infrastructure.Clients;

public sealed class EaDiagnosticsClient : IEaDiagnosticsClient
{
    private readonly HttpClient _httpClient;

    public EaDiagnosticsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EaDiagnosticProbeResult> ProbeAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(endpoint.TrimStart('/'), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new EaDiagnosticProbeResult(response.IsSuccessStatusCode, (int)response.StatusCode, response.IsSuccessStatusCode ? null : response.ReasonPhrase, body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new EaDiagnosticProbeResult(false, null, ex.Message, null);
        }
    }
}
