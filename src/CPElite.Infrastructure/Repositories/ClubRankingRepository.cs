using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class ClubRankingRepository : IClubRankingRepository
{
    private readonly CPEliteDbContext _dbContext;

    public ClubRankingRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<TeamRanking>> GetTopAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamRankings
            .Include(ranking => ranking.Team)
            .OrderBy(ranking => ranking.Rank)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClubRankingMatchInput>> GetScoredTournamentMatchesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.TournamentMatches
            .Include(match => match.Tournament)
            .Where(match => match.HomeScore.HasValue && match.AwayScore.HasValue)
            .Select(match => new ClubRankingMatchInput(
                match.TournamentId,
                match.Tournament!.StartsAt,
                match.Stage,
                match.HomeTeamId,
                match.AwayTeamId,
                match.HomeScore!.Value,
                match.AwayScore!.Value,
                match.WinnerTeamId))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ClubRankingChampionInput>> GetChampionTitlesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChampionTitles
            .Select(title => new ClubRankingChampionInput(title.TeamId, title.TournamentId))
            .ToArrayAsync(cancellationToken);
    }

    public async Task ReplaceAllAsync(IReadOnlyCollection<TeamRanking> rankings, CancellationToken cancellationToken = default)
    {
        _dbContext.TeamRankings.RemoveRange(_dbContext.TeamRankings);
        if (rankings.Count > 0)
        {
            await _dbContext.TeamRankings.AddRangeAsync(rankings, cancellationToken);
        }
    }
}
