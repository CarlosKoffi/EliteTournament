using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentRegistrationResponse(
    Guid Id,
    Guid TournamentId,
    Guid TeamId,
    TournamentPaymentMode PaymentMode,
    bool IsPaymentComplete,
    TournamentRegistrationStatus Status,
    string Source,
    string? DiscordGuildId,
    string? DiscordChannelId,
    string? DiscordMessageId,
    string? DiscordRequestedByUserId);
