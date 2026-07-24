using System.Text.Json;
using CPElite.Application.Abstractions;
using CPElite.Contracts.Common;
using CPElite.Contracts.TournamentParticipation;
using CPElite.Domain.Entities;
using DomainPresenceStatus = CPElite.Domain.Enums.TournamentPlayerPresenceStatus;

namespace CPElite.Application.Services;

public sealed class TournamentParticipationService
{
    private readonly ITournamentRepository _tournaments;
    private readonly ITournamentParticipationRepository _participants;
    private readonly ITeamRepository _teams;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public TournamentParticipationService(ITournamentRepository tournaments, ITournamentParticipationRepository participants, ITeamRepository teams, IClock clock, IUnitOfWork unitOfWork)
    {
        _tournaments = tournaments;
        _participants = participants;
        _teams = teams;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TournamentParticipantResponse>> AddParticipantAsync(Guid actorUserId, Guid tournamentId, Guid teamId, AddTournamentParticipantRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureOwnerCanManageTournamentTeamAsync(actorUserId, tournamentId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TournamentParticipantResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(request.Position))
        {
            return Result<TournamentParticipantResponse>.Failure(ErrorType.Validation, "participant.position_required", "Position is required.");
        }

        var activeMembership = await _teams.GetActiveMembershipForUserAsync(request.UserId, cancellationToken);
        if (!request.IsLoan && (activeMembership is null || activeMembership.TeamId != teamId))
        {
            return Result<TournamentParticipantResponse>.Failure(ErrorType.NotFound, "participant.member_not_found", "Player must be an active team member.");
        }

        if (request.IsLoan)
        {
            if (request.LoanFromTeamId is null)
            {
                return Result<TournamentParticipantResponse>.Failure(ErrorType.Validation, "participant.loan_team_required", "Loan source team is required.");
            }

            if (activeMembership is null || activeMembership.TeamId != request.LoanFromTeamId)
            {
                return Result<TournamentParticipantResponse>.Failure(ErrorType.Conflict, "participant.loan_source_invalid", "Loan player must belong to the declared source team.");
            }
        }

        if (await _participants.GetAsync(tournamentId, teamId, request.UserId, cancellationToken) is not null)
        {
            return Result<TournamentParticipantResponse>.Failure(ErrorType.Conflict, "participant.already_added", "Player is already listed for this tournament.");
        }

        var current = await _participants.GetTeamConfirmationsAsync(tournamentId, teamId, cancellationToken);
        if (current.Count >= 11)
        {
            return Result<TournamentParticipantResponse>.Failure(ErrorType.Conflict, "participant.roster_full", "Tournament roster already has 11 players.");
        }

        var confirmation = new TournamentPlayerConfirmation(Guid.NewGuid(), tournamentId, teamId, request.UserId, request.Position.Trim(), request.IsLoan, request.LoanFromTeamId, _clock.UtcNow);
        await _participants.AddAsync(confirmation, cancellationToken);
        await TrackParticipantEventAsync(
            tournamentId,
            teamId,
            actorUserId,
            "roster.player_added",
            "step2.prepare_roster",
            "Player added to tournament roster.",
            new { request.UserId, Position = request.Position.Trim(), request.IsLoan, request.LoanFromTeamId },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentParticipantResponse>.Success(ToResponse(confirmation));
    }

    public async Task<Result<IReadOnlyCollection<TournamentParticipantResponse>>> GetTeamParticipantsAsync(Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var confirmations = await _participants.GetTeamConfirmationsAsync(tournamentId, teamId, cancellationToken);
        return Result<IReadOnlyCollection<TournamentParticipantResponse>>.Success(confirmations.Select(ToResponse).ToArray());
    }

    public async Task<Result<TournamentParticipantReminderResponse>> RemindPendingAsync(Guid actorUserId, Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureOwnerCanManageTournamentTeamAsync(actorUserId, tournamentId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TournamentParticipantReminderResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var confirmations = await _participants.GetTeamConfirmationsAsync(tournamentId, teamId, cancellationToken);
        var pending = confirmations.Where(confirmation => confirmation.Status == DomainPresenceStatus.Pending).ToArray();
        foreach (var confirmation in pending)
        {
            confirmation.MarkReminderSent(_clock.UtcNow);
        }

        await TrackParticipantEventAsync(
            tournamentId,
            teamId,
            actorUserId,
            "presence.reminder_sent",
            "step3.final_confirmation",
            "Pending players were reminded to confirm presence.",
            new { PendingPlayers = pending.Length, UserIds = pending.Select(player => player.UserId).ToArray() },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentParticipantReminderResponse>.Success(new TournamentParticipantReminderResponse(tournamentId, teamId, pending.Length, _clock.UtcNow));
    }

    public async Task<Result<TournamentParticipantResponse>> ApproveLoanAsync(Guid actorUserId, Guid tournamentId, Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var confirmation = await _participants.GetAsync(tournamentId, teamId, userId, cancellationToken);
        if (confirmation is null || !confirmation.IsLoan || confirmation.LoanFromTeamId is null)
        {
            return Result<TournamentParticipantResponse>.Failure(ErrorType.NotFound, "participant.loan_not_found", "Loan participant was not found.");
        }

        var sourceOwner = await _teams.GetMembershipAsync(confirmation.LoanFromTeamId.Value, actorUserId, cancellationToken);
        if (sourceOwner is null || !sourceOwner.CanManageRoles())
        {
            return Result<TournamentParticipantResponse>.Failure(ErrorType.Forbidden, "participant.loan_approval_forbidden", "Only the source team owner can approve this loan.");
        }

        confirmation.ApproveLoan(actorUserId, _clock.UtcNow);
        await TrackParticipantEventAsync(
            tournamentId,
            teamId,
            actorUserId,
            "loan.approved",
            "step2.prepare_roster",
            "Loan player approved for tournament roster.",
            new { confirmation.UserId, confirmation.LoanFromTeamId },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentParticipantResponse>.Success(ToResponse(confirmation));
    }

    public async Task<Result<TournamentParticipantResponse>> UpdateMyPresenceAsync(Guid userId, Guid tournamentId, Guid teamId, UpdateMyTournamentPresenceRequest request, CancellationToken cancellationToken = default)
    {
        var confirmation = await _participants.GetAsync(tournamentId, teamId, userId, cancellationToken);
        if (confirmation is null)
        {
            return Result<TournamentParticipantResponse>.Failure(ErrorType.NotFound, "participant.not_found", "Player is not listed for this tournament.");
        }

        if (request.Status == TournamentPlayerPresenceStatus.Late && request.DelayMinutes is null or <= 0)
        {
            return Result<TournamentParticipantResponse>.Failure(ErrorType.Validation, "participant.delay_required", "Delay minutes are required when player is late.");
        }

        confirmation.UpdatePresence((DomainPresenceStatus)(int)request.Status, request.DelayMinutes, NormalizeOptional(request.Note), _clock.UtcNow);
        if (confirmation.ReplacementSuggested)
        {
            var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
            await _teams.AddPlayerDemandAsync(new TeamPlayerDemand(Guid.NewGuid(), teamId, userId, confirmation.Position, _clock.UtcNow.AddHours(1), expiresAt, $"Replacement suggested after absence for tournament {tournamentId}.", _clock.UtcNow), cancellationToken);
        }

        await TrackParticipantEventAsync(
            tournamentId,
            teamId,
            userId,
            "presence.updated",
            "player.presence",
            "Player updated tournament presence.",
            new { userId, request.Status, request.DelayMinutes, request.Note, confirmation.RequiresOwnerNotification, confirmation.ReplacementSuggested },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentParticipantResponse>.Success(ToResponse(confirmation));
    }

    private async Task<Result<bool>> EnsureOwnerCanManageTournamentTeamAsync(Guid actorUserId, Guid tournamentId, Guid teamId, CancellationToken cancellationToken)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<bool>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var membership = await _teams.GetMembershipAsync(teamId, actorUserId, cancellationToken);
        if (membership is null || !membership.IsActive)
        {
            return Result<bool>.Failure(ErrorType.Forbidden, "participant.manage_forbidden", "Only the team owner, manager or pending GM claimant can manage the tournament roster.");
        }

        var canManage = membership.CanManageRoles()
            || await _teams.GetPendingManagerClaimAsync(teamId, actorUserId, cancellationToken) is not null;

        return !canManage
            ? Result<bool>.Failure(ErrorType.Forbidden, "participant.manage_forbidden", "Only the team owner, manager or pending GM claimant can manage the tournament roster.")
            : Result<bool>.Success(true);
    }

    private static TournamentParticipantResponse ToResponse(TournamentPlayerConfirmation confirmation)
    {
        var user = confirmation.User ?? throw new InvalidOperationException("Tournament participant user was not loaded.");
        return new TournamentParticipantResponse(
            confirmation.Id,
            confirmation.TournamentId,
            confirmation.TeamId,
            user.Id,
            user.DisplayName,
            user.Gamertag,
            confirmation.Position,
            (TournamentPlayerPresenceStatus)(int)confirmation.Status,
            confirmation.DelayMinutes,
            confirmation.Note,
            confirmation.IsLoan,
            confirmation.LoanFromTeamId,
            !confirmation.IsLoan || confirmation.LoanApprovedAt is not null,
            confirmation.LoanApprovedAt,
            confirmation.LastReminderSentAt,
            confirmation.RequiresOwnerNotification,
            confirmation.ReplacementSuggested,
            confirmation.UpdatedAt);
    }

    private async Task TrackParticipantEventAsync(Guid tournamentId, Guid teamId, Guid? actorUserId, string eventType, string step, string message, object payload, CancellationToken cancellationToken)
    {
        var registration = await _tournaments.GetRegistrationAsync(tournamentId, teamId, cancellationToken);
        await _tournaments.AddRegistrationEventAsync(new TournamentRegistrationEvent(
            Guid.NewGuid(),
            tournamentId,
            teamId,
            registration?.Id,
            actorUserId,
            eventType,
            step,
            registration?.Status,
            registration?.PaymentMode,
            message,
            _clock.UtcNow,
            JsonSerializer.Serialize(payload)),
            cancellationToken);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
