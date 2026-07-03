namespace CPElite.Contracts.Teams;

public sealed record CreatePlayerDemandRequest(
    string Position,
    DateTimeOffset NeededAt,
    DateTimeOffset? ExpiresAt,
    string? Note);
