using CPElite.Contracts.Common;

namespace CPElite.Contracts.Auth;

public sealed record UserSummaryResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? Gamertag,
    string? EaSportsId,
    string? DiscordUserId,
    Platform Platform,
    string PreferredLanguage,
    string TimeZone,
    string? ProfileImageUrl = null,
    long? EaClubId = null,
    string? EaClubName = null);
