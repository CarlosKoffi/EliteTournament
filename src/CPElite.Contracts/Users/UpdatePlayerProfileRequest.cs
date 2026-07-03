using CPElite.Contracts.Common;

namespace CPElite.Contracts.Users;

public sealed record UpdatePlayerProfileRequest(
    string DisplayName,
    string? Gamertag,
    string? EaSportsId,
    Platform Platform,
    string? PreferredLanguage,
    string? TimeZone,
    string? DiscordUserId = null,
    string? ProfileImageUrl = null);
