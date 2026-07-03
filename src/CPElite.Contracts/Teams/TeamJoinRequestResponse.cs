using CPElite.Contracts.Common;

namespace CPElite.Contracts.Teams;

public sealed record TeamJoinRequestResponse(
    Guid Id,
    Guid TeamId,
    Guid UserId,
    string DisplayName,
    string? Gamertag,
    string? Message,
    JoinRequestStatus Status,
    DateTimeOffset CreatedAt);
