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
        return await _dbContext.TournamentMatches
            .Where(match =>
                match.Status == Domain.Enums.TournamentMatchStatus.Scheduled &&
                match.ScheduledAt <= startsAt &&
                match.EaLookupUntil >= now)
            .OrderBy(match => match.ScheduledAt)
            .Take(take)
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
