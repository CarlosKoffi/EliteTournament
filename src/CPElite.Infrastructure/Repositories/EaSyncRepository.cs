using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class EaSyncRepository : IEaSyncRepository
{
    private readonly CPEliteDbContext _dbContext;

    public EaSyncRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EaClubSnapshot?> GetClubSnapshotAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EaClubSnapshots.FirstOrDefaultAsync(snapshot => snapshot.TeamId == teamId, cancellationToken);
    }

    public async Task<EaMemberStatsSnapshot?> GetMemberStatsSnapshotAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EaMemberStatsSnapshots.FirstOrDefaultAsync(snapshot => snapshot.TeamId == teamId, cancellationToken);
    }

    public async Task<EaMatchSnapshot?> GetMatchSnapshotAsync(Guid teamId, string matchType, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EaMatchSnapshots.FirstOrDefaultAsync(snapshot => snapshot.TeamId == teamId && snapshot.MatchType == matchType, cancellationToken);
    }

    public async Task<IReadOnlyCollection<EaPlayerProfileSnapshot>> GetPlayerProfilesAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EaPlayerProfileSnapshots
            .Where(snapshot => snapshot.TeamId == teamId)
            .OrderByDescending(snapshot => snapshot.Goals ?? 0)
            .ThenByDescending(snapshot => snapshot.Assists ?? 0)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<EaFriendlyMatch>> GetFriendlyMatchesAsync(Guid teamId, int take = 20, CancellationToken cancellationToken = default, Guid? tournamentMatchId = null)
    {
        var query = _dbContext.EaFriendlyMatches
            .Where(match => match.TeamId == teamId);

        if (tournamentMatchId.HasValue)
        {
            query = query.Where(match => match.TournamentMatchId == tournamentMatchId.Value);
        }

        return await query
            .OrderByDescending(match => match.PlayedAt)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<EaFriendlyMatch?> GetFriendlyMatchAsync(Guid teamId, string eaMatchId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EaFriendlyMatches
            .Include(match => match.PlayerStats)
            .Include(match => match.ClubStats)
            .FirstOrDefaultAsync(match => match.TeamId == teamId && match.EaMatchId == eaMatchId, cancellationToken);
    }

    public async Task UpsertClubSnapshotAsync(EaClubSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await GetClubSnapshotAsync(snapshot.TeamId, cancellationToken);
        if (existing is null)
        {
            await _dbContext.EaClubSnapshots.AddAsync(snapshot, cancellationToken);
            return;
        }

        existing.Refresh(snapshot.Name, snapshot.Abbreviation, snapshot.Division, snapshot.MembersCount, snapshot.RawJson, snapshot.SyncedAt);
    }

    public async Task UpsertMemberStatsSnapshotAsync(EaMemberStatsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await GetMemberStatsSnapshotAsync(snapshot.TeamId, cancellationToken);
        if (existing is null)
        {
            await _dbContext.EaMemberStatsSnapshots.AddAsync(snapshot, cancellationToken);
            return;
        }

        existing.Refresh(snapshot.RawJson, snapshot.SyncedAt);
    }

    public async Task UpsertMatchSnapshotAsync(EaMatchSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = await GetMatchSnapshotAsync(snapshot.TeamId, snapshot.MatchType, cancellationToken);
        if (existing is null)
        {
            await _dbContext.EaMatchSnapshots.AddAsync(snapshot, cancellationToken);
            return;
        }

        existing.Refresh(snapshot.RawJson, snapshot.SyncedAt);
    }

    public async Task ReplacePlayerProfilesAsync(Guid teamId, IReadOnlyCollection<EaPlayerProfileSnapshot> profiles, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.EaPlayerProfileSnapshots.Where(snapshot => snapshot.TeamId == teamId).ToArrayAsync(cancellationToken);
        _dbContext.EaPlayerProfileSnapshots.RemoveRange(existing);
        if (profiles.Count > 0)
        {
            await _dbContext.EaPlayerProfileSnapshots.AddRangeAsync(profiles, cancellationToken);
        }
    }

    public async Task UpsertFriendlyMatchesAsync(Guid teamId, IReadOnlyCollection<EaFriendlyMatch> matches, IReadOnlyCollection<EaMatchPlayerStat> playerStats, IReadOnlyCollection<EaMatchClubStat> clubStats, CancellationToken cancellationToken = default)
    {
        if (matches.Count == 0)
        {
            return;
        }

        var matchIds = matches.Select(match => match.EaMatchId).ToArray();
        var existing = await _dbContext.EaFriendlyMatches
            .Where(match => match.TeamId == teamId && matchIds.Contains(match.EaMatchId))
            .ToArrayAsync(cancellationToken);
        var existingIds = existing.Select(match => match.Id).ToArray();
        var existingStats = await _dbContext.EaMatchPlayerStats
            .Where(stat => existingIds.Contains(stat.EaFriendlyMatchId))
            .ToArrayAsync(cancellationToken);
        var existingClubStats = await _dbContext.EaMatchClubStats
            .Where(stat => existingIds.Contains(stat.EaFriendlyMatchId))
            .ToArrayAsync(cancellationToken);

        _dbContext.EaMatchClubStats.RemoveRange(existingClubStats);
        _dbContext.EaMatchPlayerStats.RemoveRange(existingStats);
        _dbContext.EaFriendlyMatches.RemoveRange(existing);

        await _dbContext.EaFriendlyMatches.AddRangeAsync(matches, cancellationToken);
        if (playerStats.Count > 0)
        {
            await _dbContext.EaMatchPlayerStats.AddRangeAsync(playerStats, cancellationToken);
        }

        if (clubStats.Count > 0)
        {
            await _dbContext.EaMatchClubStats.AddRangeAsync(clubStats, cancellationToken);
        }
    }

    public async Task LinkFriendlyMatchToTournamentMatchAsync(string eaMatchId, Guid tournamentMatchId, CancellationToken cancellationToken = default)
    {
        var matches = await _dbContext.EaFriendlyMatches
            .Where(match => match.EaMatchId == eaMatchId)
            .ToArrayAsync(cancellationToken);

        foreach (var match in matches)
        {
            match.LinkToTournamentMatch(tournamentMatchId);
        }
    }
}
