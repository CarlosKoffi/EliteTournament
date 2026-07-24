using CPElite.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[AllowAnonymous]
[Route("api/rankings")]
public sealed class RankingsController : ApiControllerBase
{
    private readonly ClubRankingService _clubRankingService;

    public RankingsController(ClubRankingService clubRankingService)
    {
        _clubRankingService = clubRankingService;
    }

    [HttpGet("clubs")]
    public async Task<IActionResult> GetClubRankings([FromQuery] int take = 5, CancellationToken cancellationToken = default)
    {
        return ToActionResult(await _clubRankingService.GetTopClubsAsync(take, cancellationToken));
    }
}
