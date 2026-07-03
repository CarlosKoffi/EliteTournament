namespace CPElite.Contracts.Ea;

public sealed record EaProbeExtractedDataResponse(
    EaProbeClubDataResponse? Club,
    IReadOnlyCollection<EaProbePlayerDataResponse> Players,
    IReadOnlyCollection<EaProbeMatchDataResponse> Matches,
    IReadOnlyDictionary<string, string?> RawInterestingFields);

public sealed record EaProbeClubDataResponse(
    long? EaClubId,
    string? Name,
    string? Abbreviation,
    int? Division,
    int? MembersCount,
    int? Wins,
    int? Losses,
    int? Draws,
    int? GamesPlayed,
    int? GoalsFor,
    int? GoalsAgainst,
    int? SkillRating);

public sealed record EaProbePlayerDataResponse(
    string Name,
    string? EaSportsId,
    string? ProName,
    string? Position,
    int? Matches,
    int? WinRate,
    int? Goals,
    int? Assists,
    double? AverageRating,
    int? Height,
    int? Overall,
    int? ShotSuccessRate,
    int? PassesMade,
    int? PassesAttempted,
    double? PassAccuracy,
    int? TacklesMade,
    int? TacklesAttempted,
    double? TackleSuccess,
    int? CleanSheetsDef,
    int? CleanSheetsGk,
    int? PlayerOfTheMatch,
    int? RedCards,
    int? PrevGoals);

public sealed record EaProbeMatchDataResponse(
    string? MatchId,
    DateTimeOffset? PlayedAt,
    string? MatchType,
    string? HomeClubName,
    string? AwayClubName,
    int? HomeScore,
    int? AwayScore,
    IReadOnlyCollection<string> Signals,
    IReadOnlyCollection<EaMatchClubStatResponse> ClubStats,
    IReadOnlyCollection<EaMatchPlayerStatResponse> PlayerStats);
