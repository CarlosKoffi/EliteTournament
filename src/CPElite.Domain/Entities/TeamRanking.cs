namespace CPElite.Domain.Entities;

public sealed class TeamRanking
{
    private TeamRanking() { }

    public TeamRanking(
        Guid id,
        Guid teamId,
        int rank,
        int points,
        int tournamentsPlayed,
        int matchesPlayed,
        int wins,
        int draws,
        int losses,
        int goalsFor,
        int goalsAgainst,
        int cleanSheets,
        int titles,
        int finals,
        int podiums,
        DateTimeOffset? lastTournamentAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        TeamId = teamId;
        Rank = rank;
        Points = points;
        TournamentsPlayed = tournamentsPlayed;
        MatchesPlayed = matchesPlayed;
        Wins = wins;
        Draws = draws;
        Losses = losses;
        GoalsFor = goalsFor;
        GoalsAgainst = goalsAgainst;
        CleanSheets = cleanSheets;
        Titles = titles;
        Finals = finals;
        Podiums = podiums;
        LastTournamentAt = lastTournamentAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Team? Team { get; private set; }
    public int Rank { get; private set; }
    public int Points { get; private set; }
    public int TournamentsPlayed { get; private set; }
    public int MatchesPlayed { get; private set; }
    public int Wins { get; private set; }
    public int Draws { get; private set; }
    public int Losses { get; private set; }
    public int GoalsFor { get; private set; }
    public int GoalsAgainst { get; private set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;
    public int CleanSheets { get; private set; }
    public int Titles { get; private set; }
    public int Finals { get; private set; }
    public int Podiums { get; private set; }
    public DateTimeOffset? LastTournamentAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
