namespace CPElite.Contracts.Ea;

public sealed record EaPlayerStatsResponse(
    string Name,
    string? EaSportsId,
    int? Matches,
    int? Goals,
    int? Assists,
    double? AverageRating,
    string? Position);
