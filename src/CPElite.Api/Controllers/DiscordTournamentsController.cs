using CPElite.Application.Services;
using CPElite.Contracts.Tournaments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[AllowAnonymous]
[Route("api/discord/tournaments")]
public sealed class DiscordTournamentsController : ApiControllerBase
{
    private const string ApiKeyHeaderName = "X-CPElite-Discord-Key";
    private readonly TournamentService _tournamentService;
    private readonly IConfiguration _configuration;

    public DiscordTournamentsController(TournamentService tournamentService, IConfiguration configuration)
    {
        _tournamentService = tournamentService;
        _configuration = configuration;
    }

    [HttpGet("{tournamentId:guid}/registrations")]
    public async Task<IActionResult> GetRegistrationBoard(Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.GetRegistrationSummaryAsync(tournamentId, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/registrations/by-team-name")]
    public async Task<IActionResult> RegisterByTeamName(Guid tournamentId, DiscordTournamentRegistrationRequest request, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.RegisterTeamFromDiscordAsync(tournamentId, request, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/lock-registration")]
    public async Task<IActionResult> LockRegistration(Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.LockRegistrationFromDiscordAsync(tournamentId, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/no-shows/by-team-name/{teamName}")]
    public async Task<IActionResult> MarkNoShow(Guid tournamentId, string teamName, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.MarkNoShowFromDiscordAsync(tournamentId, teamName, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/waitlist/promote-next")]
    public async Task<IActionResult> PromoteNext(Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.PromoteNextWaitlistedTeamAsync(tournamentId, cancellationToken));
    }

    [HttpGet("moments/pending")]
    public async Task<IActionResult> GetPendingMoments([FromQuery] int take, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.GetPendingDiscordMomentsAsync(take <= 0 ? 50 : take, cancellationToken));
    }

    [HttpPost("moments/{momentId:guid}/mark-published")]
    public async Task<IActionResult> MarkMomentPublished(Guid momentId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.MarkMomentPublishedToDiscordAsync(momentId, cancellationToken));
    }

    [HttpPost("matches/{matchId:guid}/moments")]
    public async Task<IActionResult> CreateMomentFromDiscord(Guid matchId, CreateTournamentMomentRequest request, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.CreateMomentFromDiscordAsync(matchId, request, cancellationToken));
    }

    [HttpPost("matches/{matchId:guid}/verify-ea")]
    public async Task<IActionResult> VerifyMatchWithEa(Guid matchId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return ToActionResult(await _tournamentService.VerifyMatchWithEaAsync(matchId, cancellationToken));
    }

    private bool IsAuthorized()
    {
        var configuredKey = _configuration["DiscordBot:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return false;
        }

        return Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey)
            && string.Equals(providedKey.ToString(), configuredKey, StringComparison.Ordinal);
    }
}
