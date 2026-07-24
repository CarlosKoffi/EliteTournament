using CPElite.Domain.Entities;
using CPElite.Domain.Enums;

namespace CPElite.Application.Abstractions;

public interface IClubRankingRepository
{
    Task<IReadOnlyCollection<TeamRanking>> GetTopAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ClubRankingMatchInput>> GetScoredTournamentMatchesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ClubRankingChampionInput>> GetChampionTitlesAsync(CancellationToken cancellationToken = default);
    Task ReplaceAllAsync(IReadOnlyCollection<TeamRanking> rankings, CancellationToken cancellationToken = default);
}

public sealed record ClubRankingMatchInput(
    Guid TournamentId,
    DateTimeOffset TournamentStartsAt,
    TournamentStage Stage,
    Guid HomeTeamId,
    Guid AwayTeamId,
    int HomeScore,
    int AwayScore,
    Guid? WinnerTeamId);

public sealed record ClubRankingChampionInput(Guid TeamId, Guid TournamentId);
