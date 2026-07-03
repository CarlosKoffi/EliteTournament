namespace CPElite.Contracts.Tournaments;

public sealed record DiscordTournamentRegistrationRequest(
    string TeamName,
    string DiscordGuildId,
    string DiscordChannelId,
    string? DiscordMessageId,
    string RequestedByDiscordUserId);
