using CPElite.Domain.Entities;
using CPElite.Domain.Enums;

namespace CPElite.Tests.Unit.Domain;

public sealed class TeamMemberTests
{
    [Fact]
    public void Owner_can_manage_roles()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), TeamRole.Owner, DateTimeOffset.UtcNow);

        Assert.True(member.CanManageRoles());
    }

    [Fact]
    public void Player_cannot_manage_roles()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), TeamRole.Player, DateTimeOffset.UtcNow);

        Assert.False(member.CanManageRoles());
    }

    [Fact]
    public void ChangeRole_updates_member_role()
    {
        var member = TeamMember.Create(Guid.NewGuid(), Guid.NewGuid(), TeamRole.Player, DateTimeOffset.UtcNow);

        member.ChangeRole(TeamRole.Captain);

        Assert.Equal(TeamRole.Captain, member.Role);
    }
}
