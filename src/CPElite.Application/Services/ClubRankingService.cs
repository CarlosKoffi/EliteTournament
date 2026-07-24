using CPElite.Application.Abstractions;
using CPElite.Contracts.Common;
using CPElite.Contracts.Rankings;
using CPElite.Domain.Entities;

namespace CPElite.Application.Services;

public sealed class ClubRankingService
{
    private const int BasePoints = 1000;
    private readonly IClubRankingRepository _rankings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClubRankingService(IClubRankingRepository rankings, IUnitOfWork unitOfWork, IClock clock)
    {
        _rankings = rankings;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyCollection<ClubRankingResponse>>> GetTopClubsAsync(int take = 5, CancellationToken cancellationToken = default)
    {
        var rankings = await _rankings.GetTopAsync(Math.Clamp(take, 1, 50), cancellationToken);
        var response = rankings
            .OrderBy(ranking => ranking.Rank)
            .Select(ToResponse)
            .ToArray();

        return Result<IReadOnlyCollection<ClubRankingResponse>>.Success(response);
    }

    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        var matches = await _rankings.GetScoredTournamentMatchesAsync(cancellationToken);
        var titles = await _rankings.GetChampionTitlesAsync(cancellationToken);
        var statsByTeam = new Dictionary<Guid, RankingAccumulator>();

        foreach (var match in matches)
        {
            AddMatch(statsByTeam, match, match.HomeTeamId, match.HomeScore, match.AwayScore);
            AddMatch(statsByTeam, match, match.AwayTeamId, match.AwayScore, match.HomeScore);
        }

        foreach (var title in titles)
        {
            var stats = GetStats(statsByTeam, title.TeamId);
            stats.TitleTournamentIds.Add(title.TournamentId);
        }

        var now = _clock.UtcNow;
        var ranked = statsByTeam
            .Values
            .Where(stats => stats.MatchesPlayed > 0)
            .OrderByDescending(stats => stats.TotalPoints)
            .ThenByDescending(stats => stats.GoalDifference)
            .ThenByDescending(stats => stats.GoalsFor)
            .ThenBy(stats => stats.TeamId)
            .Select((stats, index) => stats.ToEntity(index + 1, now))
            .ToArray();

        await _rankings.ReplaceAllAsync(ranked, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void AddMatch(Dictionary<Guid, RankingAccumulator> statsByTeam, ClubRankingMatchInput match, Guid teamId, int goalsFor, int goalsAgainst)
    {
        var stats = GetStats(statsByTeam, teamId);
        var goalDifference = goalsFor - goalsAgainst;
        var isDraw = goalDifference == 0;
        var isWin = goalDifference > 0 || match.WinnerTeamId == teamId;

        stats.TournamentIds.Add(match.TournamentId);
        stats.LastTournamentAt = stats.LastTournamentAt is null || match.TournamentStartsAt > stats.LastTournamentAt
            ? match.TournamentStartsAt
            : stats.LastTournamentAt;
        stats.MatchesPlayed++;
        stats.GoalsFor += goalsFor;
        stats.GoalsAgainst += goalsAgainst;

        if (isDraw)
        {
            stats.Draws++;
        }
        else if (isWin)
        {
            stats.Wins++;
        }
        else
        {
            stats.Losses++;
        }

        if (goalsAgainst == 0)
        {
            stats.CleanSheets++;
        }

        if (match.Stage is Domain.Enums.TournamentStage.Final)
        {
            stats.FinalTournamentIds.Add(match.TournamentId);
        }

        if (match.Stage is Domain.Enums.TournamentStage.Final or Domain.Enums.TournamentStage.SemiFinal or Domain.Enums.TournamentStage.ThirdPlace)
        {
            stats.PodiumTournamentIds.Add(match.TournamentId);
        }

        stats.TotalPoints += CalculateMatchPoints(match.Stage, goalsFor, goalsAgainst, isWin, isDraw);
    }

    private static RankingAccumulator GetStats(Dictionary<Guid, RankingAccumulator> statsByTeam, Guid teamId)
    {
        if (!statsByTeam.TryGetValue(teamId, out var stats))
        {
            stats = new RankingAccumulator(teamId);
            statsByTeam[teamId] = stats;
        }

        return stats;
    }

    private static double CalculateMatchPoints(Domain.Enums.TournamentStage stage, int goalsFor, int goalsAgainst, bool isWin, bool isDraw)
    {
        var importance = stage switch
        {
            Domain.Enums.TournamentStage.RoundOf16 => 1.1,
            Domain.Enums.TournamentStage.QuarterFinal => 1.2,
            Domain.Enums.TournamentStage.SemiFinal => 1.35,
            Domain.Enums.TournamentStage.Final => 1.55,
            Domain.Enums.TournamentStage.ThirdPlace => 1.25,
            _ => 1.0
        };

        var resultPoints = isDraw ? 10 : isWin ? 30 : -10;
        var goalDifferenceBonus = Math.Clamp(goalsFor - goalsAgainst, -4, 4) * 3;
        var attackBonus = Math.Min(goalsFor, 5);
        var cleanSheetBonus = goalsAgainst == 0 ? 5 : 0;

        return (resultPoints + goalDifferenceBonus + attackBonus + cleanSheetBonus) * importance;
    }

    private static ClubRankingResponse ToResponse(TeamRanking ranking) =>
        new(
            ranking.Rank,
            ranking.TeamId,
            ranking.Team?.Name ?? "Equipe",
            ranking.Team?.ShortName,
            ranking.Team?.LogoUrl,
            ranking.Points,
            ranking.TournamentsPlayed,
            ranking.MatchesPlayed,
            ranking.Wins,
            ranking.Draws,
            ranking.Losses,
            ranking.GoalsFor,
            ranking.GoalsAgainst,
            ranking.GoalDifference,
            ranking.CleanSheets,
            ranking.Titles,
            ranking.Finals,
            ranking.Podiums,
            ranking.LastTournamentAt,
            ranking.UpdatedAt);

    private sealed class RankingAccumulator
    {
        public RankingAccumulator(Guid teamId)
        {
            TeamId = teamId;
        }

        public Guid TeamId { get; }
        public HashSet<Guid> TournamentIds { get; } = [];
        public HashSet<Guid> TitleTournamentIds { get; } = [];
        public HashSet<Guid> FinalTournamentIds { get; } = [];
        public HashSet<Guid> PodiumTournamentIds { get; } = [];
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int CleanSheets { get; set; }
        public double TotalPoints { get; set; } = BasePoints;
        public DateTimeOffset? LastTournamentAt { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;

        public TeamRanking ToEntity(int rank, DateTimeOffset updatedAt)
        {
            var titleBonus = TitleTournamentIds.Count * 120;
            var finalBonus = FinalTournamentIds.Except(TitleTournamentIds).Count() * 60;
            var podiumBonus = PodiumTournamentIds.Except(TitleTournamentIds).Except(FinalTournamentIds).Count() * 35;
            var points = Math.Max(0, (int)Math.Round(TotalPoints + titleBonus + finalBonus + podiumBonus));

            return new TeamRanking(
                Guid.NewGuid(),
                TeamId,
                rank,
                points,
                TournamentIds.Count,
                MatchesPlayed,
                Wins,
                Draws,
                Losses,
                GoalsFor,
                GoalsAgainst,
                CleanSheets,
                TitleTournamentIds.Count,
                FinalTournamentIds.Count,
                PodiumTournamentIds.Count,
                LastTournamentAt,
                updatedAt);
        }
    }
}
