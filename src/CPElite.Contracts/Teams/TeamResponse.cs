using CPElite.Contracts.Common;

namespace CPElite.Contracts.Teams;

public sealed record TeamResponse(
    Guid Id,
    string Name,
    string? ShortName,
    Platform Platform,
    string? Region,
    string? Description,
    long? EaClubId,
    string? LogoUrl,
    string? BannerUrl,
    string? DiscordUrl,
    string? TwitchUrl,
    string? TikTokUrl,
    string? TwitterUrl,
    bool RequireJoinApproval,
    bool IsArchived,
    string InviteCode,
    DateTimeOffset? InviteCodeExpiresAt,
    TeamMemberResponse CurrentUserMembership);
