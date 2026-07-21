using CPElite.Contracts.Common;

namespace CPElite.Contracts.Auth;

public sealed record RegisterPlayerRequest(
    string Email,
    string Password,
    string DisplayName,
    string? Gamertag,
    string? EaSportsId,
    Platform Platform,
    string? PreferredLanguage,
    string? TimeZone,
    string? DiscordUserId = null,
    long? EaClubId = null,
    string? EaClubName = null,
    bool IsEaManager = false);
