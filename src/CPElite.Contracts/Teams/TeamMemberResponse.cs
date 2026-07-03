using CPElite.Contracts.Common;

namespace CPElite.Contracts.Teams;

public sealed record TeamMemberResponse(Guid UserId, string DisplayName, string? Gamertag, TeamRole Role, MembershipStatus Status, DateTimeOffset JoinedAt, Guid? TeamId = null, string? TeamName = null, string? TeamShortName = null);
