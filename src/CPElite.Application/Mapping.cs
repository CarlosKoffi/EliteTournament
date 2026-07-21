using CPElite.Contracts.Auth;
using CPElite.Contracts.Common;
using CPElite.Contracts.Teams;
using CPElite.Domain.Entities;

namespace CPElite.Application;

internal static class Mapping
{
    public static UserSummaryResponse ToSummary(User user)
    {
        return new UserSummaryResponse(user.Id, user.Email, user.DisplayName, user.Gamertag, user.EaSportsId, user.DiscordUserId, (Platform)(int)user.Platform, user.PreferredLanguage, user.TimeZone, user.ProfileImageUrl, user.EaClubId, user.EaClubName, user.IsAdmin, user.EaIdentityVerified, user.EaIdentityVerifiedAt);
    }

    public static TeamMemberResponse ToMemberResponse(TeamMember membership)
    {
        var user = membership.User ?? throw new InvalidOperationException("Team member user was not loaded.");
        return new TeamMemberResponse(user.Id, user.DisplayName, user.Gamertag, (TeamRole)(int)membership.Role, (MembershipStatus)(int)membership.Status, membership.JoinedAt, membership.TeamId, membership.Team?.Name, membership.Team?.ShortName);
    }
}
