using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentMomentResponse(
    Guid Id,
    Guid TournamentId,
    Guid? MatchId,
    Guid? TeamId,
    Guid? UserId,
    string? DiscordUserId,
    TournamentMomentType Type,
    string Title,
    string Message,
    string? PayloadJson,
    bool IsPublishedToDiscord,
    DateTimeOffset CreatedAt);
