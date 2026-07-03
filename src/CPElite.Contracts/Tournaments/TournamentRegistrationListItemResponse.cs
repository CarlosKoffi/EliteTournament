using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentRegistrationListItemResponse(
    Guid RegistrationId,
    Guid TeamId,
    string TeamName,
    TournamentRegistrationStatus Status,
    bool IsPaymentComplete,
    string Source,
    string? DiscordGuildId,
    string? DiscordChannelId,
    string? DiscordMessageId,
    string? DiscordRequestedByUserId,
    DateTimeOffset CreatedAt);
