namespace CPElite.Contracts.Ea;

public sealed record EaClubRosterPlayerResponse(
    string EaPlayerId,
    string PlayerName,
    string? ProName,
    string? Position,
    int? Overall,
    int? Height,
    int? Weight,
    int? Matches,
    int? Goals,
    int? Assists,
    double? AverageRating,
    bool IsInApplication = false,
    Guid? ApplicationUserId = null,
    string? ApplicationDisplayName = null);
