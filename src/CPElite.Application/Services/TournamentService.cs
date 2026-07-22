using System.Text.Json;
using CPElite.Application.Abstractions;
using CPElite.Contracts.Tournaments;
using CPElite.Domain.Entities;
using DomainPaymentMode = CPElite.Domain.Enums.TournamentPaymentMode;
using DomainReconciliationStatus = CPElite.Domain.Enums.ScoreReconciliationStatus;
using DomainScoreSource = CPElite.Domain.Enums.ScoreVerificationSource;
using DomainTournamentType = CPElite.Domain.Enums.TournamentType;
using DomainScoreRecoveryMode = CPElite.Domain.Enums.TournamentScoreRecoveryMode;
using ContractMatchStatus = CPElite.Contracts.Common.TournamentMatchStatus;
using ContractReconciliationStatus = CPElite.Contracts.Common.ScoreReconciliationStatus;
using ContractPaymentMode = CPElite.Contracts.Common.TournamentPaymentMode;
using ContractRegistrationOutcome = CPElite.Contracts.Common.DiscordTournamentRegistrationOutcome;
using ContractRegistrationStatus = CPElite.Contracts.Common.TournamentRegistrationStatus;
using ContractTournamentStatus = CPElite.Contracts.Common.TournamentStatus;
using ContractTournamentType = CPElite.Contracts.Common.TournamentType;
using DomainMomentType = CPElite.Domain.Enums.TournamentMomentType;
using DomainRegistrationStatus = CPElite.Domain.Enums.TournamentRegistrationStatus;
using DomainStage = CPElite.Domain.Enums.TournamentStage;

namespace CPElite.Application.Services;

public sealed class TournamentService
{
    private readonly ITournamentRepository _tournaments;
    private readonly ITeamRepository _teams;
    private readonly IEaSyncRepository _eaSnapshots;
    private readonly ITournamentParticipationRepository _participants;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public TournamentService(ITournamentRepository tournaments, ITeamRepository teams, IEaSyncRepository eaSnapshots, ITournamentParticipationRepository participants, IClock clock, IUnitOfWork unitOfWork)
    {
        _tournaments = tournaments;
        _teams = teams;
        _eaSnapshots = eaSnapshots;
        _participants = participants;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyCollection<TournamentResponse>>> GetTournamentsAsync(CancellationToken cancellationToken = default)
    {
        var tournaments = await _tournaments.GetTournamentsAsync(cancellationToken);
        return Result<IReadOnlyCollection<TournamentResponse>>.Success(tournaments
            .Select(ToTournamentResponse)
            .ToArray());
    }

    public async Task<Result<TournamentAdminDetailResponse>> GetTournamentAdminDetailAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentAdminDetailResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        var matches = await _tournaments.GetMatchesAsync(tournamentId, cancellationToken);
        var audits = await _tournaments.GetScoreAuditsAsync(tournamentId, 150, cancellationToken);

        return Result<TournamentAdminDetailResponse>.Success(new TournamentAdminDetailResponse(
            ToTournamentResponse(tournament),
            ToRegistrationSummary(tournament, registrations, _clock.UtcNow),
            matches.Select(ToMatchResponse).ToArray(),
            audits.Select(ToScoreAuditResponse).ToArray()));
    }

    public async Task<Result<TournamentResponse>> CreateOfficialTournamentAsync(Guid createdByUserId, CreateTournamentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<TournamentResponse>.Failure(ErrorType.Validation, "tournament.name_required", "Tournament name is required.");
        }

        if (request.MaxTeams < 2)
        {
            return Result<TournamentResponse>.Failure(ErrorType.Validation, "tournament.max_teams_invalid", "Tournament must allow at least two teams.");
        }

        if (request.MinTeams < 2 || request.MinTeams > request.MaxTeams)
        {
            return Result<TournamentResponse>.Failure(ErrorType.Validation, "tournament.min_teams_invalid", "Tournament minimum teams must be between two and the tournament capacity.");
        }

        var startsAt = request.StartsAt.ToUniversalTime();
        var registrationStartsAt = request.RegistrationStartsAt?.ToUniversalTime();
        var registrationEndsAt = (request.RegistrationEndsAt ?? request.RegistrationLockAt)?.ToUniversalTime();
        if (registrationStartsAt is not null && registrationEndsAt is not null && registrationStartsAt >= registrationEndsAt)
        {
            return Result<TournamentResponse>.Failure(ErrorType.Validation, "tournament.registration_window_invalid", "Registration start date must be before registration end date.");
        }

        if (registrationEndsAt is not null && registrationEndsAt >= startsAt)
        {
            return Result<TournamentResponse>.Failure(ErrorType.Validation, "tournament.registration_end_after_start", "Registration end date must be before the tournament start date.");
        }

        var tournament = new Tournament(
            Guid.NewGuid(),
            request.Name.Trim(),
            (DomainTournamentType)(int)request.Type,
            startsAt,
            string.IsNullOrWhiteSpace(request.TimeZone) ? "Europe/Zurich" : request.TimeZone.Trim(),
            request.MinTeams,
            request.MaxTeams,
            request.EntryFee,
            string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(request.GoodiesDescription) ? null : request.GoodiesDescription.Trim(),
            registrationEndsAt,
            request.EstimatedPrizeBudget,
            request.EaMonitoringStartsMinutesBefore,
            request.EaMonitoringEndsMinutesAfter,
            NormalizeOptional(request.PlayerRestrictionsJson) ?? "{\"playerHeight\":{\"enabled\":true,\"minCm\":160,\"maxCm\":199,\"requiresProofWhenUnknown\":true}}",
            _clock.UtcNow,
            createdByUserId,
            request.Type == ContractTournamentType.Goodies,
            registrationStartsAt,
            registrationEndsAt,
            NormalizeOptional(request.BannerUrl),
            (DomainScoreRecoveryMode)(int)request.ScoreRecoveryMode,
            Math.Clamp(request.ScoreRecoveryIntervalMinutes, 1, 60),
            request.AutoPublishPerfectScore);

        tournament.OpenRegistration();
        await _tournaments.AddTournamentAsync(tournament, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentResponse>.Success(ToTournamentResponse(tournament));
    }

    public async Task<Result<TournamentRegistrationResponse>> RegisterTeamAsync(Guid actorUserId, Guid tournamentId, RegisterTeamForTournamentRequest request, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var actorMembership = await _teams.GetMembershipAsync(request.TeamId, actorUserId, cancellationToken);
        if (actorMembership is null || !actorMembership.CanManageRoles())
        {
            return Result<TournamentRegistrationResponse>.Failure(ErrorType.Forbidden, "tournament.registration_forbidden", "Only a team owner can register the team.");
        }

        if (!HasRegistrationStarted(tournament, _clock.UtcNow))
        {
            return Result<TournamentRegistrationResponse>.Failure(ErrorType.Validation, "tournament.registration_not_started", "Tournament registration is not open yet.");
        }

        if (await _tournaments.GetRegistrationAsync(tournamentId, request.TeamId, cancellationToken) is not null)
        {
            return Result<TournamentRegistrationResponse>.Failure(ErrorType.Conflict, "tournament.team_already_registered", "Team is already registered for this tournament.");
        }

        var registration = new TournamentRegistration(Guid.NewGuid(), tournamentId, request.TeamId, actorUserId, (DomainPaymentMode)(int)request.PaymentMode, _clock.UtcNow);
        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        if (ShouldWaitlist(tournament, registrations, _clock.UtcNow))
        {
            registration.Waitlist();
        }

        await _tournaments.AddRegistrationAsync(registration, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentRegistrationResponse>.Success(ToRegistrationResponse(registration));
    }

    public async Task<Result<TournamentRegistrationResponse>> ConfirmRegistrationFromAppAsync(Guid actorUserId, Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var actorMembership = await _teams.GetMembershipAsync(teamId, actorUserId, cancellationToken);
        if (actorMembership is null || !actorMembership.CanManageRoles())
        {
            return Result<TournamentRegistrationResponse>.Failure(ErrorType.Forbidden, "tournament.confirmation_forbidden", "Only a team owner can confirm the tournament registration.");
        }

        var registration = await _tournaments.GetRegistrationAsync(tournamentId, teamId, cancellationToken);
        if (registration is null)
        {
            return Result<TournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.registration_not_found", "Team is not registered for this tournament.");
        }

        registration.MarkPaymentComplete();
        if (registration.Status == DomainRegistrationStatus.Pending)
        {
            registration.Accept();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentRegistrationResponse>.Success(ToRegistrationResponse(registration));
    }

    public async Task<Result<DiscordTournamentRegistrationResponse>> RegisterTeamFromDiscordAsync(Guid tournamentId, DiscordTournamentRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TeamName))
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.Validation, "discord.team_name_required", "Team name is required.");
        }

        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        var team = await _teams.GetByNormalizedNameAsync(Normalize(request.TeamName), cancellationToken);
        if (team is null || team.IsArchived)
        {
            return Result<DiscordTournamentRegistrationResponse>.Success(new DiscordTournamentRegistrationResponse(
                ContractRegistrationOutcome.TeamNotFound,
                "Team was not found in CPElite. The owner must create/link the team in the app before Discord can register it.",
                tournament.Id,
                null,
                request.TeamName.Trim(),
                null,
                null,
                true,
                ToRegistrationSummary(tournament, registrations, _clock.UtcNow)));
        }

        var existing = registrations.FirstOrDefault(registration => registration.TeamId == team.Id);
        if (existing is not null)
        {
            return Result<DiscordTournamentRegistrationResponse>.Success(ToDiscordResponse(
                ContractRegistrationOutcome.AlreadyRegistered,
                "Team is already registered for this tournament.",
                tournament,
                team,
                existing,
                registrations,
                existing.Status is DomainRegistrationStatus.Pending or DomainRegistrationStatus.Waitlisted));
        }

        if (!HasRegistrationStarted(tournament, _clock.UtcNow))
        {
            return Result<DiscordTournamentRegistrationResponse>.Success(new DiscordTournamentRegistrationResponse(
                ContractRegistrationOutcome.TournamentClosed,
                "Tournament registration is not open yet.",
                tournament.Id,
                team.Id,
                team.Name,
                null,
                null,
                false,
                ToRegistrationSummary(tournament, registrations, _clock.UtcNow)));
        }

        if (tournament.Status == Domain.Enums.TournamentStatus.Cancelled)
        {
            return Result<DiscordTournamentRegistrationResponse>.Success(new DiscordTournamentRegistrationResponse(
                ContractRegistrationOutcome.TournamentCancelled,
                "Tournament is cancelled.",
                tournament.Id,
                team.Id,
                team.Name,
                null,
                null,
                false,
                ToRegistrationSummary(tournament, registrations, _clock.UtcNow)));
        }

        if (tournament.Status is not (Domain.Enums.TournamentStatus.RegistrationOpen or Domain.Enums.TournamentStatus.RegistrationLocked))
        {
            return Result<DiscordTournamentRegistrationResponse>.Success(new DiscordTournamentRegistrationResponse(
                ContractRegistrationOutcome.TournamentClosed,
                "Tournament registration is not open.",
                tournament.Id,
                team.Id,
                team.Name,
                null,
                null,
                false,
                ToRegistrationSummary(tournament, registrations, _clock.UtcNow)));
        }

        var registration = new TournamentRegistration(
            Guid.NewGuid(),
            tournamentId,
            team.Id,
            Guid.Empty,
            DomainPaymentMode.ClubManagerPays,
            _clock.UtcNow,
            "Discord",
            NormalizeOptional(request.DiscordGuildId),
            NormalizeOptional(request.DiscordChannelId),
            NormalizeOptional(request.DiscordMessageId),
            NormalizeOptional(request.RequestedByDiscordUserId));

        if (ShouldWaitlist(tournament, registrations, _clock.UtcNow))
        {
            registration.Waitlist();
        }

        await _tournaments.AddRegistrationAsync(registration, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedRegistrations = registrations.Concat([registration]).ToArray();
        return Result<DiscordTournamentRegistrationResponse>.Success(ToDiscordResponse(
            registration.Status == DomainRegistrationStatus.Waitlisted ? ContractRegistrationOutcome.Waitlisted : ContractRegistrationOutcome.Registered,
            registration.Status == DomainRegistrationStatus.Waitlisted
                ? "Team was added to the waiting list. It can be promoted if another team drops."
                : "Team was added from Discord. The owner still needs to complete app/payment steps.",
            tournament,
            team,
            registration,
            updatedRegistrations,
            true));
    }

    public async Task<Result<TournamentRegistrationSummaryResponse>> GetRegistrationSummaryAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentRegistrationSummaryResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        return Result<TournamentRegistrationSummaryResponse>.Success(ToRegistrationSummary(tournament, registrations, _clock.UtcNow));
    }

    public async Task<Result<DiscordTournamentRegistrationResponse>> LockRegistrationFromDiscordAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        if (CountActiveRegistrations(registrations) < tournament.MinTeams)
        {
            tournament.Cancel();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<DiscordTournamentRegistrationResponse>.Success(new DiscordTournamentRegistrationResponse(
                ContractRegistrationOutcome.MinimumNotReached,
                "Minimum team count was not reached. Tournament was cancelled and should be refunded or credited.",
                tournament.Id,
                null,
                null,
                null,
                null,
                false,
                ToRegistrationSummary(tournament, registrations, _clock.UtcNow)));
        }

        tournament.LockRegistration();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<DiscordTournamentRegistrationResponse>.Success(new DiscordTournamentRegistrationResponse(
            ContractRegistrationOutcome.Locked,
            "Tournament registration is locked. New teams will go to the waiting list.",
            tournament.Id,
            null,
            null,
            null,
            null,
            false,
            ToRegistrationSummary(tournament, registrations, _clock.UtcNow)));
    }

    public async Task<Result<DiscordTournamentRegistrationResponse>> MarkNoShowFromDiscordAsync(Guid tournamentId, string teamName, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var team = await _teams.GetByNormalizedNameAsync(Normalize(teamName), cancellationToken);
        if (team is null)
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.NotFound, "team.not_found", "Team was not found.");
        }

        var registration = await _tournaments.GetRegistrationAsync(tournamentId, team.Id, cancellationToken);
        if (registration is null)
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.registration_not_found", "Team is not registered for this tournament.");
        }

        registration.MarkNoShow();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        return Result<DiscordTournamentRegistrationResponse>.Success(ToDiscordResponse(ContractRegistrationOutcome.NoShowMarked, "Team was marked as no-show.", tournament, team, registration, registrations, false));
    }

    public async Task<Result<DiscordTournamentRegistrationResponse>> PromoteNextWaitlistedTeamAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        if (CountActiveRegistrations(registrations) >= tournament.MaxTeams)
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.Conflict, "tournament.full", "Tournament is already full.");
        }

        var next = registrations.FirstOrDefault(registration => registration.Status == DomainRegistrationStatus.Waitlisted);
        if (next is null)
        {
            return Result<DiscordTournamentRegistrationResponse>.Failure(ErrorType.NotFound, "tournament.waitlist_empty", "No teams are waiting.");
        }

        next.Accept();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var updated = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        return Result<DiscordTournamentRegistrationResponse>.Success(ToDiscordResponse(ContractRegistrationOutcome.PromotedFromWaitlist, "Next waiting team was promoted into the tournament.", tournament, next.Team!, next, updated, !next.IsPaymentComplete));
    }

    public async Task<Result<TournamentRegistrationReplacementResponse>> WithdrawRegistrationAsync(Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentRegistrationReplacementResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var registration = await _tournaments.GetRegistrationAsync(tournamentId, teamId, cancellationToken);
        if (registration is null)
        {
            return Result<TournamentRegistrationReplacementResponse>.Failure(ErrorType.NotFound, "tournament.registration_not_found", "Team is not registered for this tournament.");
        }

        if (registration.Status is DomainRegistrationStatus.Withdrawn or DomainRegistrationStatus.NoShow or DomainRegistrationStatus.Rejected)
        {
            return Result<TournamentRegistrationReplacementResponse>.Failure(ErrorType.Conflict, "tournament.registration_already_inactive", "Team is already inactive for this tournament.");
        }

        registration.Withdraw();
        var autoReplacementAttempted = IsInsideAutoReplacementWindow(tournament, _clock.UtcNow);
        TournamentRegistration? promoted = null;
        if (autoReplacementAttempted)
        {
            var registrationsBeforeSave = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
            promoted = PromoteNextWaitlistedRegistrationIfPossible(tournament, registrationsBeforeSave);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        var message = promoted is null
            ? autoReplacementAttempted
                ? "Team was withdrawn. No waiting team could be promoted automatically."
                : "Team was withdrawn. Auto replacement only runs during the 45 minutes before kickoff."
            : $"Team was withdrawn and {promoted.Team?.Name ?? "the next waiting team"} was promoted automatically.";

        return Result<TournamentRegistrationReplacementResponse>.Success(new TournamentRegistrationReplacementResponse(
            tournament.Id,
            registration.Id,
            registration.TeamId,
            registration.Team?.Name ?? "Unknown team",
            autoReplacementAttempted,
            promoted?.Id,
            promoted?.TeamId,
            promoted?.Team?.Name,
            message,
            ToRegistrationSummary(tournament, registrations, _clock.UtcNow)));
    }

    public async Task<Result<DiscordTournamentRegistrationResponse>> PromoteNextWaitlistedTeamManualAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        return await PromoteNextWaitlistedTeamAsync(tournamentId, cancellationToken);
    }

    public async Task<Result<TournamentMatchResponse>> CreateMatchAsync(Guid tournamentId, CreateTournamentMatchRequest request, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentMatchResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var match = new TournamentMatch(Guid.NewGuid(), tournamentId, request.HomeTeamId, request.AwayTeamId, request.RoundNumber, request.ScheduledAt);
        match.UpdateEaLookupWindow(request.ScheduledAt.AddMinutes(-tournament.EaMonitoringStartsMinutesBefore), request.ScheduledAt.AddMinutes(tournament.EaMonitoringEndsMinutesAfter));
        await _tournaments.AddMatchAsync(match, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentMatchResponse>.Success(ToMatchResponse(match));
    }

    public async Task<Result<TournamentDrawResponse>> LaunchWorldCupTournamentAsync(Guid tournamentId, LaunchWorldCupTournamentRequest request, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentDrawResponse>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        if (tournament.Status is Domain.Enums.TournamentStatus.Cancelled or Domain.Enums.TournamentStatus.Completed)
        {
            return Result<TournamentDrawResponse>.Failure(ErrorType.Conflict, "tournament.not_launchable", "Tournament cannot be launched in its current status.");
        }

        var existingMatches = await _tournaments.GetMatchesAsync(tournamentId, cancellationToken);
        if (existingMatches.Any(match => match.Stage == DomainStage.Group))
        {
            return Result<TournamentDrawResponse>.Failure(ErrorType.Conflict, "tournament.draw_exists", "Tournament group draw already exists.");
        }

        var registrations = await _tournaments.GetRegistrationsAsync(tournamentId, cancellationToken);
        var eligible = registrations
            .Where(registration => registration.Status is DomainRegistrationStatus.Accepted or DomainRegistrationStatus.Pending)
            .Where(registration => registration.Team is not null)
            .OrderBy(registration => registration.CreatedAt)
            .ToArray();

        if (eligible.Length < tournament.MinTeams)
        {
            return Result<TournamentDrawResponse>.Failure(ErrorType.Conflict, "tournament.minimum_not_reached", $"Tournament needs at least {tournament.MinTeams} teams before launch.");
        }

        if (eligible.Length > tournament.MaxTeams)
        {
            eligible = eligible.Take(tournament.MaxTeams).ToArray();
        }

        var groupSize = Math.Clamp(request.GroupSize, 3, 6);
        var qualifiersPerGroup = Math.Clamp(request.QualifiersPerGroup, 1, groupSize - 1);
        var groupCount = (int)Math.Ceiling(eligible.Length / (double)groupSize);
        if (groupCount < 2)
        {
            return Result<TournamentDrawResponse>.Failure(ErrorType.Validation, "tournament.group_count_invalid", "Tournament group stage needs at least two groups.");
        }

        var shuffled = Shuffle(eligible, request.DrawSeed);
        var groups = BuildBalancedGroups(shuffled, groupCount);
        var matches = new List<TournamentMatch>();
        var matchNumber = 1;
        var scheduledAt = tournament.StartsAt;
        var minutesBetweenMatches = Math.Clamp(request.MinutesBetweenGroupMatches, 10, 60);

        foreach (var group in groups)
        {
            var roundNumber = 1;
            for (var i = 0; i < group.Teams.Count; i++)
            {
                for (var j = i + 1; j < group.Teams.Count; j++)
                {
                    var match = new TournamentMatch(Guid.NewGuid(), tournamentId, group.Teams[i].TeamId, group.Teams[j].TeamId, roundNumber, scheduledAt);
                    match.AssignDrawMetadata(DomainStage.Group, group.Name, matchNumber);
                    match.UpdateEaLookupWindow(scheduledAt.AddMinutes(-tournament.EaMonitoringStartsMinutesBefore), scheduledAt.AddMinutes(tournament.EaMonitoringEndsMinutesAfter));
                    await _tournaments.AddMatchAsync(match, cancellationToken);
                    matches.Add(match);
                    matchNumber++;
                    scheduledAt = scheduledAt.AddMinutes(minutesBetweenMatches);
                    roundNumber++;
                }
            }
        }

        tournament.LockRegistration();
        tournament.Start();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TournamentDrawResponse>.Success(new TournamentDrawResponse(
            tournament.Id,
            tournament.Name,
            eligible.Length,
            groups.Count,
            qualifiersPerGroup,
            groups.Select(group => new TournamentGroupResponse(group.Name, group.Teams)).ToArray(),
            matches.Select(ToMatchResponse).ToArray(),
            BuildKnockoutPlan(groups.Count * qualifiersPerGroup)));
    }

    public async Task<Result<TournamentMatchResponse>> SubmitManualScoreAsync(Guid actorUserId, Guid matchId, SubmitMatchScoreRequest request, CancellationToken cancellationToken = default)
    {
        var match = await _tournaments.GetMatchAsync(matchId, cancellationToken);
        if (match is null)
        {
            return Result<TournamentMatchResponse>.Failure(ErrorType.NotFound, "tournament.match_not_found", "Match was not found.");
        }

        var canSubmit = await _teams.GetMembershipAsync(match.HomeTeamId, actorUserId, cancellationToken) is { } home && home.CanManageRoles()
            || await _teams.GetMembershipAsync(match.AwayTeamId, actorUserId, cancellationToken) is { } away && away.CanManageRoles();

        if (!canSubmit)
        {
            return Result<TournamentMatchResponse>.Failure(ErrorType.Forbidden, "tournament.score_forbidden", "Only a participating team owner can submit a score.");
        }

        var winnerTeamId = ResolveWinner(match, request.HomeScore, request.AwayScore);
        match.ApplyVerifiedScore(request.HomeScore, request.AwayScore, winnerTeamId, DomainScoreSource.ClubOwner, null, _clock.UtcNow);
        await _tournaments.AddScoreSubmissionAsync(new MatchScoreSubmission(Guid.NewGuid(), matchId, actorUserId, request.HomeScore, request.AwayScore, DomainScoreSource.ClubOwner, request.ProofUrl, _clock.UtcNow), cancellationToken);
        await _tournaments.AddMomentAsync(new TournamentMoment(Guid.NewGuid(), match.TournamentId, match.Id, winnerTeamId, null, null, DomainMomentType.ScoreConfirmed, "Score confirmed", $"Score confirmed: {request.HomeScore}-{request.AwayScore}.", null, _clock.UtcNow), cancellationToken);
        await _tournaments.AddMomentAsync(CreateRandomPostMatchMoment(match, winnerTeamId), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentMatchResponse>.Success(ToMatchResponse(match));
    }

    public async Task<Result<TournamentMomentResponse>> CreateMomentAsync(Guid actorUserId, Guid matchId, CreateTournamentMomentRequest request, CancellationToken cancellationToken = default)
    {
        var match = await _tournaments.GetMatchAsync(matchId, cancellationToken);
        if (match is null)
        {
            return Result<TournamentMomentResponse>.Failure(ErrorType.NotFound, "tournament.match_not_found", "Match was not found.");
        }

        var canCreate = await _teams.GetMembershipAsync(match.HomeTeamId, actorUserId, cancellationToken) is { } home && home.CanManageRoles()
            || await _teams.GetMembershipAsync(match.AwayTeamId, actorUserId, cancellationToken) is { } away && away.CanManageRoles();

        if (!canCreate)
        {
            return Result<TournamentMomentResponse>.Failure(ErrorType.Forbidden, "tournament.moment_forbidden", "Only a participating team owner can create match moments.");
        }

        return await CreateMomentCoreAsync(match, request, cancellationToken);
    }

    public async Task<Result<TournamentMomentResponse>> CreateMomentFromDiscordAsync(Guid matchId, CreateTournamentMomentRequest request, CancellationToken cancellationToken = default)
    {
        var match = await _tournaments.GetMatchAsync(matchId, cancellationToken);
        if (match is null)
        {
            return Result<TournamentMomentResponse>.Failure(ErrorType.NotFound, "tournament.match_not_found", "Match was not found.");
        }

        return await CreateMomentCoreAsync(match, request, cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<TournamentMomentResponse>>> GetPendingDiscordMomentsAsync(int take, CancellationToken cancellationToken = default)
    {
        var moments = await _tournaments.GetPendingDiscordMomentsAsync(Math.Clamp(take, 1, 100), cancellationToken);
        return Result<IReadOnlyCollection<TournamentMomentResponse>>.Success(moments.Select(ToMomentResponse).ToArray());
    }

    public async Task<Result<TournamentMomentResponse>> MarkMomentPublishedToDiscordAsync(Guid momentId, CancellationToken cancellationToken = default)
    {
        var moment = await _tournaments.GetMomentAsync(momentId, cancellationToken);
        if (moment is null)
        {
            return Result<TournamentMomentResponse>.Failure(ErrorType.NotFound, "tournament.moment_not_found", "Tournament moment was not found.");
        }

        moment.MarkPublishedToDiscord(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentMomentResponse>.Success(ToMomentResponse(moment));
    }

    public async Task<Result<EaMatchVerificationResponse>> VerifyMatchWithEaAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return await VerifyMatchWithEaAsync(matchId, "manual-admin", cancellationToken);
    }

    public async Task<Result<EaMatchVerificationResponse>> VerifyMatchWithEaAsync(Guid matchId, string trigger, CancellationToken cancellationToken = default)
    {
        var match = await _tournaments.GetMatchAsync(matchId, cancellationToken);
        if (match is null)
        {
            return Result<EaMatchVerificationResponse>.Failure(ErrorType.NotFound, "tournament.match_not_found", "Match was not found.");
        }

        match.MarkWaitingForEaData();

        var homeTeam = await _teams.GetByIdAsync(match.HomeTeamId, cancellationToken);
        var awayTeam = await _teams.GetByIdAsync(match.AwayTeamId, cancellationToken);
        if (homeTeam?.EaClubId is null || awayTeam?.EaClubId is null)
        {
            match.RequireOwnerConfirmation();
            await _tournaments.AddScoreAuditAsync(await BuildScoreAuditAsync(match, null, trigger, cancellationToken), cancellationToken);
            await _tournaments.AddMomentAsync(new TournamentMoment(Guid.NewGuid(), match.TournamentId, match.Id, null, null, null, DomainMomentType.ProofRequested, "EA club link missing", "EA validation needs both teams to have an EA club ID linked. Club owners should confirm score and proofs manually.", null, _clock.UtcNow), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<EaMatchVerificationResponse>.Success(new EaMatchVerificationResponse(match.Id, ContractMatchStatus.OwnerConfirmationRequired, false, "EA club IDs are missing. Club owners must confirm the result.", null, null, null, true));
        }

        var tournament = await _tournaments.GetTournamentAsync(match.TournamentId, cancellationToken);
        var candidate = await FindStoredFriendlyCandidateAsync(match, tournament, homeTeam.EaClubId.Value, awayTeam.EaClubId.Value, cancellationToken)
            ?? await FindEaMatchCandidateAsync(match, homeTeam.EaClubId.Value, awayTeam.EaClubId.Value, tournament?.PlayerRestrictionsJson, cancellationToken);
        var audit = await BuildScoreAuditAsync(match, candidate, trigger, cancellationToken);
        await _tournaments.AddScoreAuditAsync(audit, cancellationToken);

        if (candidate is null || candidate.Confidence < 80)
        {
            match.RequireOwnerConfirmation();
            await _tournaments.AddMomentAsync(new TournamentMoment(Guid.NewGuid(), match.TournamentId, match.Id, null, null, null, DomainMomentType.ProofRequested, "Owner validation needed", "EA could not confidently find the friendly match yet. Owners should confirm score and requested proofs.", candidate?.PayloadJson, _clock.UtcNow), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<EaMatchVerificationResponse>.Success(new EaMatchVerificationResponse(match.Id, ContractMatchStatus.OwnerConfirmationRequired, false, candidate?.Message ?? "EA auto-validation could not confidently identify the match yet.", candidate?.Confidence, candidate?.HomeScore, candidate?.AwayScore, true, candidate?.Evidence));
        }

        var winnerTeamId = ResolveWinner(match, candidate.HomeScore, candidate.AwayScore);
        if (audit.Status == DomainReconciliationStatus.PerfectMatch && tournament?.AutoPublishPerfectScore == true)
        {
            match.ApplyVerifiedScore(candidate.HomeScore, candidate.AwayScore, winnerTeamId, DomainScoreSource.EaApi, candidate.PayloadJson, _clock.UtcNow);
        }
        else
        {
            match.CaptureEaCandidateScore(candidate.HomeScore, candidate.AwayScore, winnerTeamId, candidate.PayloadJson, _clock.UtcNow);
        }

        if (!string.IsNullOrWhiteSpace(candidate.EaMatchId))
        {
            await _eaSnapshots.LinkFriendlyMatchToTournamentMatchAsync(candidate.EaMatchId, match.Id, cancellationToken);
        }

        await _tournaments.AddMomentAsync(new TournamentMoment(Guid.NewGuid(), match.TournamentId, match.Id, winnerTeamId, null, null, DomainMomentType.ProofRequested, "EA match candidate found", "EA found a likely match. Club owners should validate the score before it is pushed as official.", candidate.PayloadJson, _clock.UtcNow), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<EaMatchVerificationResponse>.Success(new EaMatchVerificationResponse(match.Id, ContractMatchStatus.OwnerConfirmationRequired, false, "EA found a likely match candidate. Club owners must validate it before publication.", candidate.Confidence, candidate.HomeScore, candidate.AwayScore, true, candidate.Evidence));
    }

    public async Task<Result<IReadOnlyCollection<EaMatchVerificationResponse>>> VerifyTournamentScoresAsync(Guid tournamentId, string trigger = "manual-admin", CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<IReadOnlyCollection<EaMatchVerificationResponse>>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var matches = await _tournaments.GetMatchesAsync(tournamentId, cancellationToken);
        var responses = new List<EaMatchVerificationResponse>();
        foreach (var match in matches.Where(match => match.Status is Domain.Enums.TournamentMatchStatus.Scheduled or Domain.Enums.TournamentMatchStatus.WaitingForEaData or Domain.Enums.TournamentMatchStatus.OwnerConfirmationRequired or Domain.Enums.TournamentMatchStatus.ScoreSubmitted))
        {
            var result = await VerifyMatchWithEaAsync(match.Id, trigger, cancellationToken);
            if (result.Value is not null)
            {
                responses.Add(result.Value);
            }
        }

        return Result<IReadOnlyCollection<EaMatchVerificationResponse>>.Success(responses);
    }

    public async Task<Result<IReadOnlyCollection<TournamentScoreAuditResponse>>> GetScoreAuditsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournaments.GetTournamentAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<IReadOnlyCollection<TournamentScoreAuditResponse>>.Failure(ErrorType.NotFound, "tournament.not_found", "Tournament was not found.");
        }

        var audits = await _tournaments.GetScoreAuditsAsync(tournamentId, 150, cancellationToken);
        return Result<IReadOnlyCollection<TournamentScoreAuditResponse>>.Success(audits.Select(ToScoreAuditResponse).ToArray());
    }

    public async Task<int> VerifyDueEaMatchesAsync(int take = 25, CancellationToken cancellationToken = default)
    {
        var dueMatches = await _tournaments.GetEaVerificationDueMatchesAsync(_clock.UtcNow, Math.Clamp(take, 1, 100), cancellationToken);
        var checkedCount = 0;

        foreach (var match in dueMatches)
        {
            await VerifyMatchWithEaAsync(match.Id, "automatic-cron", cancellationToken);
            checkedCount++;
        }

        return checkedCount;
    }

    private async Task<EaMatchCandidate?> FindStoredFriendlyCandidateAsync(TournamentMatch match, Tournament? tournament, long homeEaClubId, long awayEaClubId, CancellationToken cancellationToken)
    {
        var candidates = await _eaSnapshots.GetFriendlyMatchesForLookupAsync(match.HomeTeamId, match.EaLookupFrom, match.EaLookupUntil, homeEaClubId, awayEaClubId, cancellationToken);
        if (candidates.Count == 0)
        {
            candidates = await _eaSnapshots.GetFriendlyMatchesForLookupAsync(match.AwayTeamId, match.EaLookupFrom, match.EaLookupUntil, homeEaClubId, awayEaClubId, cancellationToken);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .Select(candidate => ToStoredCandidate(match, candidate, candidates.Count, homeEaClubId, awayEaClubId, tournament?.PlayerRestrictionsJson))
            .OrderByDescending(candidate => candidate.Confidence)
            .ThenByDescending(candidate => candidate.PlayedAt)
            .FirstOrDefault();
    }

    private static EaMatchCandidate ToStoredCandidate(TournamentMatch tournamentMatch, EaFriendlyMatch match, int duplicateCount, long homeEaClubId, long awayEaClubId, string? restrictionsJson)
    {
        var isSameOrder = match.HomeEaClubId == homeEaClubId && match.AwayEaClubId == awayEaClubId;
        var homeScore = isSameOrder ? match.HomeScore : match.AwayScore;
        var awayScore = isSameOrder ? match.AwayScore : match.HomeScore;
        var issues = new List<string>();
        var confidence = 65;

        if (match.PlayedAt >= tournamentMatch.EaLookupFrom && match.PlayedAt <= tournamentMatch.EaLookupUntil)
        {
            confidence += 15;
        }
        else
        {
            confidence -= 25;
            issues.Add("Le timestamp du match EA est hors fenetre de recherche.");
        }

        var durationLooksComplete = LooksComplete(match.PlayerStats);
        if (durationLooksComplete)
        {
            confidence += 10;
        }
        else
        {
            confidence -= 20;
            issues.Add("La duree/statistiques joueurs ne prouvent pas encore un match complet. Possible restart ou match incomplet.");
        }

        if (duplicateCount > 1)
        {
            confidence -= 10;
            issues.Add("Plusieurs matchs candidats existent dans la fenetre, probablement restart/rematch. Verification admin conseillee.");
        }

        if (!string.IsNullOrWhiteSpace(restrictionsJson) && !match.RawJson.Contains("height", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("Restrictions tournoi non verifiables dans ce payload EA, preuve admin possible.");
        }

        var evidence = string.Join(" ", new[]
        {
            $"Friendly stocke trouve: {match.EaMatchId}.",
            $"EA clubs: {match.HomeEaClubId} vs {match.AwayEaClubId}.",
            $"Timestamp: {match.PlayedAt:O}.",
            duplicateCount > 1 ? $"{duplicateCount} candidats dans la fenetre." : "Candidat unique dans la fenetre."
        }.Concat(issues));

        return new EaMatchCandidate(
            Math.Clamp(confidence, 0, 100),
            homeScore,
            awayScore,
            match.RawJson,
            evidence,
            issues.Count == 0 ? "Friendly EA stocke retrouve et coherent." : "Friendly EA retrouve avec points a verifier.",
            match.EaMatchId,
            match.PlayedAt,
            durationLooksComplete,
            duplicateCount,
            match.PlayerStats.ToArray());
    }

    private async Task<TournamentScoreAudit> BuildScoreAuditAsync(TournamentMatch match, EaMatchCandidate? candidate, string trigger, CancellationToken cancellationToken)
    {
        if (candidate is null)
        {
            return new TournamentScoreAudit(
                Guid.NewGuid(),
                match.TournamentId,
                match.Id,
                _clock.UtcNow,
                trigger,
                DomainReconciliationStatus.NoCandidateFound,
                "Aucun match EA candidat trouve dans la fenetre configuree.",
                null,
                0,
                null,
                null,
                null,
                match.HomeScore,
                match.AwayScore,
                false,
                false,
                false,
                false,
                false,
                false,
                JsonSerializer.Serialize(new[] { "Aucun friendly EA ne correspond aux deux clubs et au timestamp attendu." }),
                JsonSerializer.Serialize(new { match.EaLookupFrom, match.EaLookupUntil }),
                null);
        }

        var homePlayers = await _participants.GetTeamConfirmationsAsync(match.TournamentId, match.HomeTeamId, cancellationToken);
        var awayPlayers = await _participants.GetTeamConfirmationsAsync(match.TournamentId, match.AwayTeamId, cancellationToken);
        var playerIssues = BuildPlayerIssues(homePlayers, awayPlayers, candidate.PlayerStats);
        var scoreMatched = match.HomeScore is null || match.AwayScore is null || (match.HomeScore == candidate.HomeScore && match.AwayScore == candidate.AwayScore);
        var timestampMatched = candidate.PlayedAt is not null && candidate.PlayedAt >= match.EaLookupFrom && candidate.PlayedAt <= match.EaLookupUntil;
        var teamsMatched = candidate.Evidence.Contains("EA clubs", StringComparison.OrdinalIgnoreCase);
        var duplicateDetected = candidate.CandidateCount > 1;
        var issues = new List<string>();

        if (!timestampMatched)
        {
            issues.Add("Timestamp EA hors fenetre attendue.");
        }

        if (!scoreMatched)
        {
            issues.Add($"Score manuel different: manuel {match.HomeScore}-{match.AwayScore}, EA {candidate.HomeScore}-{candidate.AwayScore}.");
        }

        if (!candidate.DurationLooksComplete)
        {
            issues.Add("Duree du match non conforme ou impossible a confirmer.");
        }

        if (duplicateDetected)
        {
            issues.Add("Plusieurs matchs candidats dans la fenetre, possible restart/rematch.");
        }

        issues.AddRange(playerIssues);
        var playersMatched = playerIssues.Count == 0;
        var perfect = candidate.Confidence >= 90 && timestampMatched && teamsMatched && scoreMatched && playersMatched && candidate.DurationLooksComplete && !duplicateDetected;
        var status = perfect
            ? DomainReconciliationStatus.PerfectMatch
            : candidate.Confidence >= 80 && teamsMatched && timestampMatched
                ? DomainReconciliationStatus.NeedsReview
                : DomainReconciliationStatus.Dispute;
        var summary = perfect
            ? "Match EA parfaitement rapproche: score, clubs, timestamp, duree et joueurs conformes."
            : "Rapprochement EA a verifier: des ecarts sont disponibles dans le journal de litige.";

        return new TournamentScoreAudit(
            Guid.NewGuid(),
            match.TournamentId,
            match.Id,
            _clock.UtcNow,
            trigger,
            status,
            summary,
            candidate.EaMatchId,
            candidate.CandidateCount,
            candidate.Confidence,
            candidate.HomeScore,
            candidate.AwayScore,
            match.HomeScore,
            match.AwayScore,
            teamsMatched,
            timestampMatched,
            scoreMatched,
            playersMatched,
            candidate.DurationLooksComplete,
            duplicateDetected,
            JsonSerializer.Serialize(issues),
            JsonSerializer.Serialize(new { candidate.Evidence, candidate.PlayedAt, match.EaLookupFrom, match.EaLookupUntil }),
            candidate.PayloadJson);
    }

    private static List<string> BuildPlayerIssues(IReadOnlyCollection<TournamentPlayerConfirmation> homePlayers, IReadOnlyCollection<TournamentPlayerConfirmation> awayPlayers, IReadOnlyCollection<EaMatchPlayerStat> actualPlayers)
    {
        var issues = new List<string>();
        AddPlayerIssues("home", homePlayers, actualPlayers, issues);
        AddPlayerIssues("away", awayPlayers, actualPlayers, issues);
        return issues;
    }

    private static void AddPlayerIssues(string side, IReadOnlyCollection<TournamentPlayerConfirmation> expected, IReadOnlyCollection<EaMatchPlayerStat> actualPlayers, List<string> issues)
    {
        if (expected.Count == 0)
        {
            issues.Add($"Aucun joueur annonce cote {side}, verification joueurs impossible.");
            return;
        }

        var actualKeys = actualPlayers
            .SelectMany(stat => BuildPlayerKeys(stat.EaPlayerId, stat.PlayerName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var confirmation in expected)
        {
            var expectedKeys = BuildPlayerKeys(confirmation.User?.EaSportsId, confirmation.User?.Gamertag, confirmation.User?.DisplayName);
            if (!expectedKeys.Any(key => actualKeys.Contains(key)))
            {
                issues.Add($"Joueur annonce non retrouve cote {side}: {confirmation.User?.Gamertag ?? confirmation.User?.DisplayName ?? confirmation.UserId.ToString()}.");
            }
        }
    }

    private static IReadOnlyCollection<string> BuildPlayerKeys(params string?[] values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool LooksComplete(IReadOnlyCollection<EaMatchPlayerStat> playerStats)
    {
        if (playerStats.Count == 0)
        {
            return false;
        }

        return playerStats.Any(stat => stat.SecondsPlayed is >= 540 || stat.GameTime is >= 540 || stat.RealtimeGame is >= 540);
    }

    private static Guid? ResolveWinner(TournamentMatch match, int homeScore, int awayScore)
    {
        if (homeScore == awayScore)
        {
            return null;
        }

        return homeScore > awayScore ? match.HomeTeamId : match.AwayTeamId;
    }

    private static List<TournamentRegistration> Shuffle(IReadOnlyCollection<TournamentRegistration> registrations, int? seed)
    {
        var random = seed is null ? Random.Shared : new Random(seed.Value);
        var shuffled = registrations.ToList();
        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }

    private static List<DrawGroup> BuildBalancedGroups(IReadOnlyList<TournamentRegistration> registrations, int groupCount)
    {
        var groups = Enumerable
            .Range(0, groupCount)
            .Select(index => new DrawGroup($"Group {(char)('A' + index)}", []))
            .ToList();

        for (var index = 0; index < registrations.Count; index++)
        {
            var registration = registrations[index];
            groups[index % groupCount].Teams.Add(new TournamentGroupTeamResponse(registration.TeamId, registration.Team?.Name ?? "Unknown team", index + 1));
        }

        return groups;
    }

    private static IReadOnlyCollection<TournamentKnockoutRoundPlanResponse> BuildKnockoutPlan(int qualifiedTeams)
    {
        var plan = new List<TournamentKnockoutRoundPlanResponse>();
        var nextPowerOfTwo = 1;
        while (nextPowerOfTwo < qualifiedTeams)
        {
            nextPowerOfTwo *= 2;
        }

        if (nextPowerOfTwo >= 16)
        {
            plan.Add(new TournamentKnockoutRoundPlanResponse(CPElite.Contracts.Common.TournamentStage.RoundOf16, 8, "Round of 16"));
        }

        if (nextPowerOfTwo >= 8)
        {
            plan.Add(new TournamentKnockoutRoundPlanResponse(CPElite.Contracts.Common.TournamentStage.QuarterFinal, 4, "Quarter-finals"));
        }

        if (nextPowerOfTwo >= 4)
        {
            plan.Add(new TournamentKnockoutRoundPlanResponse(CPElite.Contracts.Common.TournamentStage.SemiFinal, 2, "Semi-finals"));
        }

        plan.Add(new TournamentKnockoutRoundPlanResponse(CPElite.Contracts.Common.TournamentStage.ThirdPlace, 1, "Third place match"));
        plan.Add(new TournamentKnockoutRoundPlanResponse(CPElite.Contracts.Common.TournamentStage.Final, 1, "Final"));
        return plan;
    }

    private sealed record DrawGroup(string Name, List<TournamentGroupTeamResponse> Teams);

    private static TournamentResponse ToTournamentResponse(Tournament tournament)
    {
        return new TournamentResponse(tournament.Id, tournament.Name, (ContractTournamentType)(int)tournament.Type, (ContractTournamentStatus)(int)tournament.Status, tournament.StartsAt, tournament.TimeZone, tournament.MinTeams, tournament.MaxTeams, tournament.EntryFee, tournament.Currency, tournament.GoodiesDescription, tournament.RegistrationLockAt, tournament.EstimatedPrizeBudget, tournament.EaMonitoringStartsMinutesBefore, tournament.EaMonitoringEndsMinutesAfter, tournament.PlayerRestrictionsJson, tournament.IsCashPrize, tournament.RegistrationStartsAt, tournament.RegistrationEndsAt, tournament.BannerUrl, (CPElite.Contracts.Common.TournamentScoreRecoveryMode)(int)tournament.ScoreRecoveryMode, tournament.ScoreRecoveryIntervalMinutes, tournament.AutoPublishPerfectScore);
    }

    private static TournamentRegistrationResponse ToRegistrationResponse(TournamentRegistration registration)
    {
        return new TournamentRegistrationResponse(registration.Id, registration.TournamentId, registration.TeamId, (ContractPaymentMode)(int)registration.PaymentMode, registration.IsPaymentComplete, (ContractRegistrationStatus)(int)registration.Status, registration.Source, registration.DiscordGuildId, registration.DiscordChannelId, registration.DiscordMessageId, registration.DiscordRequestedByUserId);
    }

    private static TournamentMatchResponse ToMatchResponse(TournamentMatch match)
    {
        return new TournamentMatchResponse(match.Id, match.TournamentId, match.HomeTeamId, match.AwayTeamId, match.RoundNumber, match.ScheduledAt, match.EaLookupFrom, match.EaLookupUntil, match.HomeScore, match.AwayScore, (ContractMatchStatus)(int)match.Status, match.WinnerTeamId, (CPElite.Contracts.Common.TournamentStage)(int)match.Stage, match.GroupName, match.MatchNumber);
    }

    private static TournamentScoreAuditResponse ToScoreAuditResponse(TournamentScoreAudit audit)
    {
        return new TournamentScoreAuditResponse(
            audit.Id,
            audit.TournamentId,
            audit.TournamentMatchId,
            audit.AttemptedAt,
            audit.Trigger,
            (ContractReconciliationStatus)(int)audit.Status,
            audit.Summary,
            audit.EaMatchId,
            audit.CandidateCount,
            audit.Confidence,
            audit.HomeScore,
            audit.AwayScore,
            audit.ManualHomeScore,
            audit.ManualAwayScore,
            audit.TeamsMatched,
            audit.TimestampMatched,
            audit.ScoreMatched,
            audit.PlayersMatched,
            audit.DurationLooksComplete,
            audit.DuplicateCandidateDetected,
            audit.IssuesJson,
            audit.EvidenceJson);
    }

    private async Task<Result<TournamentMomentResponse>> CreateMomentCoreAsync(TournamentMatch match, CreateTournamentMomentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            return Result<TournamentMomentResponse>.Failure(ErrorType.Validation, "tournament.moment_text_required", "Moment title and message are required.");
        }

        var moment = new TournamentMoment(
            Guid.NewGuid(),
            match.TournamentId,
            match.Id,
            request.TeamId,
            request.UserId,
            NormalizeOptional(request.DiscordUserId),
            (DomainMomentType)(int)request.Type,
            request.Title.Trim(),
            request.Message.Trim(),
            NormalizeOptional(request.PayloadJson),
            _clock.UtcNow);

        await _tournaments.AddMomentAsync(moment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TournamentMomentResponse>.Success(ToMomentResponse(moment));
    }

    private static TournamentMomentResponse ToMomentResponse(TournamentMoment moment)
    {
        return new TournamentMomentResponse(
            moment.Id,
            moment.TournamentId,
            moment.MatchId,
            moment.TeamId,
            moment.UserId,
            moment.DiscordUserId,
            (CPElite.Contracts.Common.TournamentMomentType)(int)moment.Type,
            moment.Title,
            moment.Message,
            moment.PayloadJson,
            moment.IsPublishedToDiscord,
            moment.CreatedAt);
    }

    private TournamentMoment CreateRandomPostMatchMoment(TournamentMatch match, Guid? winnerTeamId)
    {
        var templates = new[]
        {
            (DomainMomentType.WellDone, "Well done", "Great match. Take a breath, check the result, and get ready for the next one."),
            (DomainMomentType.PlayerOfTheMatch, "Player of the match", "Owners can nominate the player of the match from the app."),
            (DomainMomentType.NextMatchReady, "Next game", "The next opponent will be announced as soon as the bracket is ready."),
            (DomainMomentType.LeaderboardUpdate, "Ranking update", "Stats and rankings will update after validation.")
        };
        var selected = templates[Random.Shared.Next(templates.Length)];

        return new TournamentMoment(Guid.NewGuid(), match.TournamentId, match.Id, winnerTeamId, null, null, selected.Item1, selected.Item2, selected.Item3, null, _clock.UtcNow);
    }

    private async Task<EaMatchCandidate?> FindEaMatchCandidateAsync(TournamentMatch match, long homeEaClubId, long awayEaClubId, string? restrictionsJson, CancellationToken cancellationToken)
    {
        var friendlyLookupFrom = match.ScheduledAt.AddMinutes(-10);
        var friendlyLookupUntil = match.ScheduledAt.AddMinutes(10);
        var normalizedFriendlies = (await _eaSnapshots.GetFriendlyMatchesAsync(match.HomeTeamId, 50, cancellationToken))
            .Concat(await _eaSnapshots.GetFriendlyMatchesAsync(match.AwayTeamId, 50, cancellationToken));

        EaMatchCandidate? normalizedBest = null;
        foreach (var friendly in normalizedFriendlies)
        {
            var hasBothClubs = friendly.HomeEaClubId == homeEaClubId && friendly.AwayEaClubId == awayEaClubId
                || friendly.HomeEaClubId == awayEaClubId && friendly.AwayEaClubId == homeEaClubId;
            if (!hasBothClubs)
            {
                continue;
            }

            var confidence = 70;
            var reasons = new List<string> { "Friendly match found in normalized EA data with both expected clubs." };
            if (friendly.PlayedAt >= friendlyLookupFrom && friendly.PlayedAt <= friendlyLookupUntil)
            {
                confidence += 25;
                reasons.Add("Friendly kickoff is inside scheduledAt +/- 10 minutes.");
            }
            else
            {
                confidence -= 25;
                reasons.Add("Friendly kickoff is outside scheduledAt +/- 10 minutes.");
            }

            var homeScore = friendly.HomeEaClubId == homeEaClubId ? friendly.HomeScore : friendly.AwayScore;
            var awayScore = friendly.HomeEaClubId == awayEaClubId ? friendly.HomeScore : friendly.AwayScore;
            confidence += 10;
            reasons.Add("Score was extracted from normalized friendly data.");

            var candidate = new EaMatchCandidate(
                Math.Clamp(confidence, 0, 100),
                homeScore,
                awayScore,
                friendly.RawJson,
                string.Join(" ", reasons),
                confidence >= 80 ? "Likely friendly match found." : "Friendly candidate is not confident enough.",
                friendly.EaMatchId);

            if (normalizedBest is null || candidate.Confidence > normalizedBest.Confidence)
            {
                normalizedBest = candidate;
            }
        }

        if (normalizedBest is not null)
        {
            return normalizedBest;
        }

        var snapshots = new[]
        {
            await _eaSnapshots.GetMatchSnapshotAsync(match.HomeTeamId, "friendlyMatch", cancellationToken),
            await _eaSnapshots.GetMatchSnapshotAsync(match.AwayTeamId, "friendlyMatch", cancellationToken),
            await _eaSnapshots.GetMatchSnapshotAsync(match.HomeTeamId, "leagueMatch", cancellationToken),
            await _eaSnapshots.GetMatchSnapshotAsync(match.AwayTeamId, "leagueMatch", cancellationToken),
            await _eaSnapshots.GetMatchSnapshotAsync(match.HomeTeamId, "playoffMatch", cancellationToken),
            await _eaSnapshots.GetMatchSnapshotAsync(match.AwayTeamId, "playoffMatch", cancellationToken)
        }.Where(snapshot => snapshot is not null).Cast<EaMatchSnapshot>();

        EaMatchCandidate? best = null;
        foreach (var snapshot in snapshots)
        {
            foreach (var candidate in ParseEaMatchCandidates(snapshot.RawJson, homeEaClubId, awayEaClubId, match.EaLookupFrom, match.EaLookupUntil, restrictionsJson))
            {
                if (best is null || candidate.Confidence > best.Confidence)
                {
                    best = candidate;
                }
            }
        }

        return best;
    }

    private static IEnumerable<EaMatchCandidate> ParseEaMatchCandidates(string rawJson, long homeEaClubId, long awayEaClubId, DateTimeOffset lookupFrom, DateTimeOffset lookupUntil, string? restrictionsJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        foreach (var matchElement in EnumeratePotentialMatchObjects(document.RootElement))
        {
            var json = matchElement.GetRawText();
            if (!json.Contains(homeEaClubId.ToString(), StringComparison.Ordinal) || !json.Contains(awayEaClubId.ToString(), StringComparison.Ordinal))
            {
                continue;
            }

            var confidence = 55;
            var reasons = new List<string> { "Both EA club IDs were found in the same match payload." };

            var playedAt = FindFirstDate(matchElement);
            if (playedAt is not null && playedAt >= lookupFrom && playedAt <= lookupUntil)
            {
                confidence += 25;
                reasons.Add("Match time is inside the configured monitoring window.");
            }
            else if (playedAt is not null)
            {
                confidence -= 20;
                reasons.Add("Match time is outside the configured monitoring window.");
            }
            else
            {
                reasons.Add("EA payload did not expose a clear match time.");
            }

            var completion = EvaluateCompletion(matchElement);
            confidence += completion.ConfidenceDelta;
            reasons.Add(completion.Reason);

            var homeScore = FindScoreForClub(matchElement, homeEaClubId);
            var awayScore = FindScoreForClub(matchElement, awayEaClubId);
            if (homeScore is not null && awayScore is not null)
            {
                confidence += 20;
                reasons.Add("Scores were extracted for both EA clubs.");
            }
            else
            {
                homeScore ??= FindFirstIntByName(matchElement, "homeScore", "score1", "club1Score", "team1Score") ?? 0;
                awayScore ??= FindFirstIntByName(matchElement, "awayScore", "score2", "club2Score", "team2Score") ?? 0;
                confidence -= 10;
                reasons.Add("Scores were inferred from generic score fields.");
            }

            var restrictionEvidence = EvaluateRestrictionEvidence(matchElement, restrictionsJson);
            confidence += restrictionEvidence.ConfidenceDelta;
            reasons.AddRange(restrictionEvidence.Reasons);

            yield return new EaMatchCandidate(
                Math.Clamp(confidence, 0, 100),
                homeScore.Value,
                awayScore.Value,
                json,
                string.Join(" ", reasons),
                confidence >= 80 ? "Likely EA match found." : "EA match candidate is not confident enough.",
                FindFirstStringByName(matchElement, "matchId", "id", "gameId"));
        }
    }

    private static IEnumerable<JsonElement> EnumeratePotentialMatchObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var raw = element.GetRawText();
            if ((raw.Contains("score", StringComparison.OrdinalIgnoreCase) || raw.Contains("goals", StringComparison.OrdinalIgnoreCase))
                && raw.Contains("club", StringComparison.OrdinalIgnoreCase)
                && HasDirectMatchSignals(element))
            {
                yield return element;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var child in EnumeratePotentialMatchObjects(property.Value))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in EnumeratePotentialMatchObjects(item))
                {
                    yield return child;
                }
            }
        }
    }

    private static DateTimeOffset? FindFirstDate(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.Name.Contains("date", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("time", StringComparison.OrdinalIgnoreCase))
                    && property.Value.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParse(property.Value.GetString(), out var parsed))
                {
                    return parsed;
                }

                var nested = FindFirstDate(property.Value);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindFirstDate(item);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static bool HasDirectMatchSignals(JsonElement element)
    {
        var hasClubProperty = false;
        var hasMatchIdentity = false;

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Contains("club", StringComparison.OrdinalIgnoreCase))
            {
                hasClubProperty = true;
            }

            if (property.Name.Contains("match", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("date", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("time", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("status", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("duration", StringComparison.OrdinalIgnoreCase))
            {
                hasMatchIdentity = true;
            }
        }

        return hasClubProperty && hasMatchIdentity;
    }

    private static int? FindScoreForClub(JsonElement element, long eaClubId)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (DirectClubIdMatches(element, eaClubId))
            {
                return FindDirectIntByName(element, "score", "goals", "goalsFor", "clubScore");
            }

            foreach (var property in element.EnumerateObject())
            {
                if ((string.Equals(property.Name, eaClubId.ToString(), StringComparison.OrdinalIgnoreCase) || DirectClubIdMatches(property.Value, eaClubId))
                    && property.Value.ValueKind == JsonValueKind.Object)
                {
                    var score = FindDirectIntByName(property.Value, "score", "goals", "goalsFor", "clubScore");
                    if (score is not null)
                    {
                        return score;
                    }
                }

                var nested = FindScoreForClub(property.Value, eaClubId);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindScoreForClub(item, eaClubId);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static bool DirectClubIdMatches(JsonElement element, long eaClubId)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.Contains("club", StringComparison.OrdinalIgnoreCase) || !property.Name.Contains("id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt64(out var number) && number == eaClubId)
            {
                return true;
            }

            if (property.Value.ValueKind == JsonValueKind.String && string.Equals(property.Value.GetString(), eaClubId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int? FindDirectIntByName(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) && property.Value.TryGetInt32(out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static int? FindFirstIntByName(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) && property.Value.TryGetInt32(out var value))
                {
                    return value;
                }

                var nested = FindFirstIntByName(property.Value, names);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindFirstIntByName(item, names);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string? FindFirstStringByName(JsonElement element, params string[] names)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Number => property.Value.GetRawText(),
                        _ => null
                    };
                }

                var nested = FindFirstStringByName(property.Value, names);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindFirstStringByName(item, names);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static CompletionEvidence EvaluateCompletion(JsonElement matchElement)
    {
        var raw = matchElement.GetRawText();
        if (raw.Contains("full", StringComparison.OrdinalIgnoreCase) || raw.Contains("finished", StringComparison.OrdinalIgnoreCase) || raw.Contains("complete", StringComparison.OrdinalIgnoreCase))
        {
            return new CompletionEvidence(15, "Match appears to be finished/full time.");
        }

        if (raw.Contains("restart", StringComparison.OrdinalIgnoreCase) || raw.Contains("abandon", StringComparison.OrdinalIgnoreCase) || raw.Contains("quit", StringComparison.OrdinalIgnoreCase))
        {
            return new CompletionEvidence(-35, "Match appears to be restarted, abandoned, or incomplete.");
        }

        var duration = FindFirstIntByName(matchElement, "durationMinutes", "minutesPlayed", "matchLengthMinutes");
        if (duration is >= 10)
        {
            return new CompletionEvidence(10, "Match duration looks complete.");
        }

        if (duration is > 0 and < 10)
        {
            return new CompletionEvidence(-30, "Match duration is too short and may be a restart.");
        }

        return new CompletionEvidence(0, "Match completion status was not explicit.");
    }

    private static RestrictionEvidence EvaluateRestrictionEvidence(JsonElement matchElement, string? restrictionsJson)
    {
        if (string.IsNullOrWhiteSpace(restrictionsJson))
        {
            return new RestrictionEvidence(0, ["No tournament restriction config was provided."]);
        }

        var raw = matchElement.GetRawText();
        if (raw.Contains("height", StringComparison.OrdinalIgnoreCase) || raw.Contains("taille", StringComparison.OrdinalIgnoreCase))
        {
            return new RestrictionEvidence(5, ["Player restriction fields appear to be present and should be reviewed by owners."]);
        }

        return new RestrictionEvidence(-5, ["Player restriction fields were not found in the EA match payload, so proof may be required."]);
    }

    private sealed record EaMatchCandidate(int Confidence, int HomeScore, int AwayScore, string PayloadJson, string Evidence, string Message, string? EaMatchId = null, DateTimeOffset? PlayedAt = null, bool DurationLooksComplete = false, int CandidateCount = 1, IReadOnlyCollection<EaMatchPlayerStat>? PlayerStats = null)
    {
        public IReadOnlyCollection<EaMatchPlayerStat> PlayerStats { get; init; } = PlayerStats ?? Array.Empty<EaMatchPlayerStat>();
    }

    private sealed record CompletionEvidence(int ConfidenceDelta, string Reason);

    private sealed record RestrictionEvidence(int ConfidenceDelta, IReadOnlyCollection<string> Reasons);

    private static DiscordTournamentRegistrationResponse ToDiscordResponse(ContractRegistrationOutcome outcome, string message, Tournament tournament, Team team, TournamentRegistration registration, IReadOnlyCollection<TournamentRegistration> registrations, bool requiresAppAction)
    {
        return new DiscordTournamentRegistrationResponse(
            outcome,
            message,
            tournament.Id,
            team.Id,
            team.Name,
            registration.Id,
            (ContractRegistrationStatus)(int)registration.Status,
            requiresAppAction,
            ToRegistrationSummary(tournament, registrations, DateTimeOffset.UtcNow));
    }

    private static TournamentRegistrationSummaryResponse ToRegistrationSummary(Tournament tournament, IReadOnlyCollection<TournamentRegistration> registrations, DateTimeOffset now)
    {
        var activeTeams = CountActiveRegistrations(registrations);
        var teams = registrations
            .OrderBy(registration => registration.Status == DomainRegistrationStatus.Waitlisted ? 1 : 0)
            .ThenBy(registration => registration.CreatedAt)
            .Select(registration => new TournamentRegistrationListItemResponse(
                registration.Id,
                registration.TeamId,
                registration.Team?.Name ?? "Unknown team",
                (ContractRegistrationStatus)(int)registration.Status,
                registration.IsPaymentComplete,
                registration.Source,
                registration.DiscordGuildId,
                registration.DiscordChannelId,
                registration.DiscordMessageId,
                registration.DiscordRequestedByUserId,
                registration.CreatedAt))
            .ToArray();

        return new TournamentRegistrationSummaryResponse(
            tournament.Id,
            tournament.Name,
            tournament.StartsAt,
            tournament.RegistrationStartsAt,
            tournament.RegistrationEndsAt,
            tournament.RegistrationLockAt,
            tournament.MinTeams,
            tournament.MaxTeams,
            tournament.EntryFee,
            tournament.Currency,
            activeTeams,
            registrations.Count(registration => registration.Status == DomainRegistrationStatus.Pending),
            registrations.Count(registration => registration.Status == DomainRegistrationStatus.Accepted),
            registrations.Count(registration => registration.Status == DomainRegistrationStatus.Waitlisted),
            registrations.Count(registration => registration.Status == DomainRegistrationStatus.NoShow),
            activeTeams >= tournament.MinTeams,
            activeTeams >= tournament.MaxTeams,
            tournament.Status == Domain.Enums.TournamentStatus.RegistrationLocked || (tournament.RegistrationEndsAt is not null && now >= tournament.RegistrationEndsAt) || (tournament.RegistrationLockAt is not null && now >= tournament.RegistrationLockAt),
            teams);
    }

    private static bool HasRegistrationStarted(Tournament tournament, DateTimeOffset now)
    {
        return tournament.RegistrationStartsAt is null || now >= tournament.RegistrationStartsAt;
    }

    private static bool ShouldWaitlist(Tournament tournament, IReadOnlyCollection<TournamentRegistration> registrations, DateTimeOffset now)
    {
        return tournament.Status == Domain.Enums.TournamentStatus.RegistrationLocked
            || (tournament.RegistrationEndsAt is not null && now >= tournament.RegistrationEndsAt)
            || (tournament.RegistrationLockAt is not null && now >= tournament.RegistrationLockAt)
            || CountActiveRegistrations(registrations) >= tournament.MaxTeams;
    }

    private static bool IsInsideAutoReplacementWindow(Tournament tournament, DateTimeOffset now)
    {
        return now >= tournament.StartsAt.AddMinutes(-45) && now < tournament.StartsAt;
    }

    private static TournamentRegistration? PromoteNextWaitlistedRegistrationIfPossible(Tournament tournament, IReadOnlyCollection<TournamentRegistration> registrations)
    {
        if (CountActiveRegistrations(registrations) >= tournament.MaxTeams)
        {
            return null;
        }

        var next = registrations
            .Where(registration => registration.Status == DomainRegistrationStatus.Waitlisted)
            .OrderBy(registration => registration.CreatedAt)
            .FirstOrDefault();

        next?.Accept();
        return next;
    }

    private static int CountActiveRegistrations(IReadOnlyCollection<TournamentRegistration> registrations)
    {
        return registrations.Count(registration => registration.Status is DomainRegistrationStatus.Pending or DomainRegistrationStatus.Accepted);
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
