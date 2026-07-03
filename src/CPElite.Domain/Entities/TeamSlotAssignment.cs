namespace CPElite.Domain.Entities;

public sealed class TeamSlotAssignment
{
    private TeamSlotAssignment() { }

    public TeamSlotAssignment(Guid id, Guid teamSlotPackageId, Guid teamId, Guid userId, Guid assignedByUserId, DateTimeOffset assignedAt)
    {
        Id = id;
        TeamSlotPackageId = teamSlotPackageId;
        TeamId = teamId;
        UserId = userId;
        AssignedByUserId = assignedByUserId;
        AssignedAt = assignedAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamSlotPackageId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public TeamSlotPackage? Package { get; private set; }
    public Team? Team { get; private set; }
    public User? User { get; private set; }

    public bool IsActive => ReleasedAt is null;

    public void Release(DateTimeOffset releasedAt)
    {
        ReleasedAt = releasedAt;
    }
}
