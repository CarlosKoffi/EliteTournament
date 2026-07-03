using CPElite.Application.Services;
using CPElite.Contracts.TournamentParticipation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/tournaments/{tournamentId:guid}/teams/{teamId:guid}/participants")]
public sealed class TournamentParticipantsController : ApiControllerBase
{
    private readonly TournamentParticipationService _participationService;

    public TournamentParticipantsController(TournamentParticipationService participationService)
    {
        _participationService = participationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeamParticipants(Guid tournamentId, Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _participationService.GetTeamParticipantsAsync(tournamentId, teamId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddParticipant(Guid tournamentId, Guid teamId, AddTournamentParticipantRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _participationService.AddParticipantAsync(CurrentUserId, tournamentId, teamId, request, cancellationToken));
    }

    [HttpPost("remind-pending")]
    public async Task<IActionResult> RemindPending(Guid tournamentId, Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _participationService.RemindPendingAsync(CurrentUserId, tournamentId, teamId, cancellationToken));
    }

    [HttpPost("{userId:guid}/loan/approve")]
    public async Task<IActionResult> ApproveLoan(Guid tournamentId, Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _participationService.ApproveLoanAsync(CurrentUserId, tournamentId, teamId, userId, cancellationToken));
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMyPresence(Guid tournamentId, Guid teamId, UpdateMyTournamentPresenceRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _participationService.UpdateMyPresenceAsync(CurrentUserId, tournamentId, teamId, request, cancellationToken));
    }
}
