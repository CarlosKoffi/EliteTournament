using CPElite.Contracts.Auth;
using CPElite.Contracts.Teams;

namespace CPElite.Contracts.Users;

public sealed record MeResponse(UserSummaryResponse User, IReadOnlyCollection<TeamMemberResponse> Memberships);
