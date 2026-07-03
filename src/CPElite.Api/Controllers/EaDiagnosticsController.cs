using CPElite.Application.Services;
using CPElite.Contracts.Ea;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/ea/diagnostics")]
public sealed class EaDiagnosticsController : ApiControllerBase
{
    private readonly EaDiagnosticsService _diagnosticsService;

    public EaDiagnosticsController(EaDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService;
    }

    [HttpPost("probe")]
    public async Task<IActionResult> Probe(EaProbeRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _diagnosticsService.ProbeAsync(request, cancellationToken));
    }
}
