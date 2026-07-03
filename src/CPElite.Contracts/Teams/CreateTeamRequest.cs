using CPElite.Contracts.Common;

namespace CPElite.Contracts.Teams;

public sealed record CreateTeamRequest(
    string Name,
    string? ShortName,
    Platform Platform,
    string? Region,
    string? Description,
    long? EaClubId,
    string? LogoUrl,
    string? BannerUrl);
