using CPElite.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[AllowAnonymous]
[Route("api/ea/clubs")]
public sealed class EaClubsController : ApiControllerBase
{
    private readonly EaClubDiscoveryService _discoveryService;

    public EaClubsController(EaClubDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string clubName, [FromQuery] string? platform, CancellationToken cancellationToken)
    {
        return ToActionResult(await _discoveryService.SearchClubAsync(clubName, platform, cancellationToken));
    }

    [HttpGet("{clubId:long}/roster")]
    public async Task<IActionResult> GetRoster(long clubId, [FromQuery] string? platform, CancellationToken cancellationToken)
    {
        return ToActionResult(await _discoveryService.GetClubRosterAsync(clubId, platform, cancellationToken));
    }

    [HttpGet("/api/ea/players/search")]
    public async Task<IActionResult> SearchPlayer([FromQuery] string eaSportsId, [FromQuery] string? platform, [FromQuery] long? clubId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _discoveryService.SearchPlayerAsync(eaSportsId, platform, clubId, cancellationToken));
    }
}
