using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record CreateTournamentMomentRequest(
    TournamentMomentType Type,
    Guid? TeamId,
    Guid? UserId,
    string? DiscordUserId,
    string Title,
    string Message,
    string? PayloadJson);
