using CPElite.Contracts.Common;

namespace CPElite.Contracts.Teams;

public sealed record PlayerDemandResponse(
    Guid Id,
    Guid TeamId,
    string TeamName,
    string? TeamShortName,
    string? TeamLogoUrl,
    string Position,
    DateTimeOffset NeededAt,
    DateTimeOffset ExpiresAt,
    string? Note,
    PlayerDemandStatus Status,
    DateTimeOffset CreatedAt);
