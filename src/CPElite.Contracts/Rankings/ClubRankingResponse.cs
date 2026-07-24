namespace CPElite.Contracts.Rankings;

public sealed record ClubRankingResponse(
    int Rank,
    Guid TeamId,
    string TeamName,
    string? ShortName,
    string? LogoUrl,
    int Points,
    int TournamentsPlayed,
    int MatchesPlayed,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int CleanSheets,
    int Titles,
    int Finals,
    int Podiums,
    DateTimeOffset? LastTournamentAt,
    DateTimeOffset UpdatedAt);
