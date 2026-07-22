using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class TournamentRepository : ITournamentRepository
{
    private readonly CPEliteDbContext _dbContext;

    public TournamentRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Tournament>> GetTournamentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tournaments
            .OrderBy(tournament => tournament.StartsAt)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Tournament?> GetTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tournaments.FirstOrDefaultAsync(tournament => tournament.Id == tournamentId, cancellationToken);
    }

    public Task<TournamentMatch?> GetMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentMatches.FirstOrDefaultAsync(match => match.Id == matchId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TournamentMatch>> GetMatchesAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TournamentMatches
            .Where(match => match.TournamentId == tournamentId)
            .OrderBy(match => match.RoundNumber)
            .ThenBy(match => match.MatchNumber)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TournamentMatch>> GetEaVerificationDueMatchesAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default)
    {
        var startsAt = now.AddMinutes(-15);
        var candidates = await _dbContext.TournamentMatches
            .Include(match => match.Tournament)
            .Where(match =>
                (match.Status == Domain.Enums.TournamentMatchStatus.Scheduled ||
                    match.Status == Domain.Enums.TournamentMatchStatus.WaitingForEaData ||
                    match.Status == Domain.Enums.TournamentMatchStatus.OwnerConfirmationRequired) &&
                match.ScheduledAt <= startsAt &&
                match.EaLookupUntil >= now &&
                match.Tournament!.ScoreRecoveryMode != Domain.Enums.TournamentScoreRecoveryMode.ManualOnly &&
                match.Tournament.ScoreRecoveryMode != Domain.Enums.TournamentScoreRecoveryMode.EndOfRound &&
                match.Tournament.ScoreRecoveryMode != Domain.Enums.TournamentScoreRecoveryMode.EndOfTournament)
            .OrderBy(match => match.ScheduledAt)
            .ToArrayAsync(cancellationToken);

        var matchIds = candidates.Select(match => match.Id).ToArray();
        var recentAudits = await _dbContext.TournamentScoreAudits
            .Where(audit => matchIds.Contains(audit.TournamentMatchId))
            .GroupBy(audit => audit.TournamentMatchId)
            .Select(group => new { MatchId = group.Key, LastAttemptedAt = group.Max(audit => audit.AttemptedAt) })
            .ToDictionaryAsync(item => item.MatchId, item => item.LastAttemptedAt, cancellationToken);

        return candidates
            .Where(match =>
            {
                if (!recentAudits.TryGetValue(match.Id, out var lastAttempt))
                {
                    return true;
                }

                var interval = Math.Clamp(match.Tournament?.ScoreRecoveryIntervalMinutes ?? 2, 1, 60);
                return lastAttempt <= now.AddMinutes(-interval);
            })
            .Take(take)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TournamentScoreAudit>> GetScoreAuditsAsync(Guid tournamentId, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TournamentScoreAudits
            .Where(audit => audit.TournamentId == tournamentId)
            .OrderByDescending(audit => audit.AttemptedAt)
            .Take(Math.Clamp(take, 1, 250))
            .ToArrayAsync(cancellationToken);
    }

    public Task<TournamentRegistration?> GetRegistrationAsync(Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentRegistrations
            .Include(registration => registration.Team)
            .FirstOrDefaultAsync(registration => registration.TournamentId == tournamentId && registration.TeamId == teamId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TournamentRegistration>> GetRegistrationsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TournamentRegistrations
            .Include(registration => registration.Team)
            .Where(registration => registration.TournamentId == tournamentId)
            .OrderBy(registration => registration.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddTournamentAsync(Tournament tournament, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tournaments.AddAsync(tournament, cancellationToken);
    }

    public async Task AddRegistrationAsync(TournamentRegistration registration, CancellationToken cancellationToken = default)
    {
        await _dbContext.TournamentRegistrations.AddAsync(registration, cancellationToken);
    }

    public async Task AddMatchAsync(TournamentMatch match, CancellationToken cancellationToken = default)
    {
        await _dbContext.TournamentMatches.AddAsync(match, cancellationToken);
    }

    public async Task AddScoreSubmissionAsync(MatchScoreSubmission submission, CancellationToken cancellationToken = default)
    {
        await _dbContext.MatchScoreSubmissions.AddAsync(submission, cancellationToken);
    }

    public async Task AddChampionTitleAsync(ChampionTitle championTitle, CancellationToken cancellationToken = default)
    {
        await _dbContext.ChampionTitles.AddAsync(championTitle, cancellationToken);
    }

    public async Task AddMomentAsync(TournamentMoment moment, CancellationToken cancellationToken = default)
    {
        await _dbContext.TournamentMoments.AddAsync(moment, cancellationToken);
    }

    public async Task AddScoreAuditAsync(TournamentScoreAudit audit, CancellationToken cancellationToken = default)
    {
        await _dbContext.TournamentScoreAudits.AddAsync(audit, cancellationToken);
    }

    public Task<TournamentMoment?> GetMomentAsync(Guid momentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentMoments.FirstOrDefaultAsync(moment => moment.Id == momentId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TournamentMoment>> GetPendingDiscordMomentsAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TournamentMoments
            .Where(moment => !moment.IsPublishedToDiscord)
            .OrderBy(moment => moment.CreatedAt)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }
}
