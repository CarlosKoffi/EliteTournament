using CPElite.Application.Services;
using CPElite.Contracts.Tournaments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/tournaments")]
public sealed class TournamentsController : ApiControllerBase
{
    private readonly TournamentService _tournamentService;

    public TournamentsController(TournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.GetTournamentsAsync(cancellationToken));
    }

    [HttpPost("official")]
    public async Task<IActionResult> CreateOfficial(CreateTournamentRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.CreateOfficialTournamentAsync(CurrentUserId, request, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/registrations")]
    public async Task<IActionResult> RegisterTeam(Guid tournamentId, RegisterTeamForTournamentRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.RegisterTeamAsync(CurrentUserId, tournamentId, request, cancellationToken));
    }

    [HttpGet("{tournamentId:guid}/registrations")]
    public async Task<IActionResult> GetRegistrationBoard(Guid tournamentId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.GetRegistrationSummaryAsync(tournamentId, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/registrations/{teamId:guid}/app-confirmation")]
    public async Task<IActionResult> ConfirmFromApp(Guid tournamentId, Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.ConfirmRegistrationFromAppAsync(CurrentUserId, tournamentId, teamId, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/registrations/{teamId:guid}/withdraw")]
    public async Task<IActionResult> WithdrawRegistration(Guid tournamentId, Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.WithdrawRegistrationAsync(tournamentId, teamId, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/registrations/waitlist/promote-next")]
    public async Task<IActionResult> PromoteNextWaitlisted(Guid tournamentId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.PromoteNextWaitlistedTeamManualAsync(tournamentId, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/matches")]
    public async Task<IActionResult> CreateMatch(Guid tournamentId, CreateTournamentMatchRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.CreateMatchAsync(tournamentId, request, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/launch-world-cup")]
    public async Task<IActionResult> LaunchWorldCup(Guid tournamentId, LaunchWorldCupTournamentRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.LaunchWorldCupTournamentAsync(tournamentId, request, cancellationToken));
    }

    [HttpPost("{tournamentId:guid}/scores/recover")]
    public async Task<IActionResult> RecoverTournamentScores(Guid tournamentId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.VerifyTournamentScoresAsync(tournamentId, "manual-admin", cancellationToken));
    }

    [HttpGet("{tournamentId:guid}/scores/audits")]
    public async Task<IActionResult> GetTournamentScoreAudits(Guid tournamentId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.GetScoreAuditsAsync(tournamentId, cancellationToken));
    }

    [HttpPost("matches/{matchId:guid}/scores")]
    public async Task<IActionResult> SubmitScore(Guid matchId, SubmitMatchScoreRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.SubmitManualScoreAsync(CurrentUserId, matchId, request, cancellationToken));
    }

    [HttpPost("matches/{matchId:guid}/moments")]
    public async Task<IActionResult> CreateMoment(Guid matchId, CreateTournamentMomentRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.CreateMomentAsync(CurrentUserId, matchId, request, cancellationToken));
    }

    [HttpPost("matches/{matchId:guid}/verify-ea")]
    public async Task<IActionResult> VerifyWithEa(Guid matchId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _tournamentService.VerifyMatchWithEaAsync(matchId, cancellationToken));
    }
}
