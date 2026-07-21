using CPElite.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/teams/{teamId:guid}/ea")]
public sealed class EaSyncController : ApiControllerBase
{
    private readonly EaSyncService _eaSyncService;

    public EaSyncController(EaSyncService eaSyncService)
    {
        _eaSyncService = eaSyncService;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(Guid teamId, [FromQuery] bool force, CancellationToken cancellationToken)
    {
        return ToActionResult(await _eaSyncService.SyncTeamAsync(teamId, force, cancellationToken));
    }

    [HttpGet("club")]
    public async Task<IActionResult> GetClubSnapshot(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _eaSyncService.GetClubSnapshotAsync(teamId, cancellationToken));
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMemberStats(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _eaSyncService.GetMemberStatsAsync(teamId, cancellationToken));
    }

    [HttpGet("players")]
    public async Task<IActionResult> GetPlayerProfiles(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _eaSyncService.GetPlayerProfilesAsync(teamId, cancellationToken));
    }

    [HttpPost("/api/ea/clubs/{eaClubId:long}/friendlies/import")]
    public async Task<IActionResult> ImportFriendlyMatchesByEaClubId(long eaClubId, [FromQuery] string? platform, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawJson = await reader.ReadToEndAsync(cancellationToken);
        return ToActionResult(await _eaSyncService.ImportFriendlyMatchesJsonByEaClubIdAsync(eaClubId, rawJson, platform, cancellationToken));
    }
    [HttpPost("friendlies/import")]
    public async Task<IActionResult> ImportFriendlyMatches(Guid teamId, [FromQuery] string? platform, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawJson = await reader.ReadToEndAsync(cancellationToken);
        return ToActionResult(await _eaSyncService.ImportFriendlyMatchesJsonAsync(teamId, rawJson, platform, cancellationToken));
    }
    [HttpGet("friendlies")]
    public async Task<IActionResult> GetFriendlyMatches(Guid teamId, [FromQuery] int take, [FromQuery] Guid? tournamentMatchId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _eaSyncService.GetFriendlyMatchesAsync(teamId, take <= 0 ? 20 : take, tournamentMatchId, cancellationToken));
    }

    [HttpGet("friendlies/{eaMatchId}")]
    public async Task<IActionResult> GetFriendlyMatch(Guid teamId, string eaMatchId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _eaSyncService.GetFriendlyMatchAsync(teamId, eaMatchId, cancellationToken));
    }
}
