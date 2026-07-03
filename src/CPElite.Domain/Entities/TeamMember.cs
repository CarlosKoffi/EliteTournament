using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TeamMember
{
    private TeamMember() { }

    private TeamMember(Guid id, Guid teamId, Guid userId, TeamRole role, DateTimeOffset joinedAt)
    {
        Id = id;
        TeamId = teamId;
        UserId = userId;
        Role = role;
        Status = MembershipStatus.Active;
        JoinedAt = joinedAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public TeamRole Role { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public Team? Team { get; private set; }
    public User? User { get; private set; }

    public static TeamMember Create(Guid teamId, Guid userId, TeamRole role, DateTimeOffset joinedAt)
    {
        return new TeamMember(Guid.NewGuid(), teamId, userId, role, joinedAt);
    }

    public bool IsActive => Status == MembershipStatus.Active;

    public bool CanManageRoles()
    {
        return IsActive && Role == TeamRole.Owner;
    }

    public bool CanManageTeamAccess()
    {
        return IsActive && Role is TeamRole.Owner or TeamRole.Manager;
    }

    public void ChangeRole(TeamRole role)
    {
        Role = role;
    }

    public void Remove()
    {
        Status = MembershipStatus.Removed;
    }
}
