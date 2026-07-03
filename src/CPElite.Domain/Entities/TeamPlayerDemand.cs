using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TeamPlayerDemand
{
    private TeamPlayerDemand() { }

    public TeamPlayerDemand(Guid id, Guid teamId, Guid createdByUserId, string position, DateTimeOffset neededAt, DateTimeOffset expiresAt, string? note, DateTimeOffset createdAt)
    {
        Id = id;
        TeamId = teamId;
        CreatedByUserId = createdByUserId;
        Position = position;
        NeededAt = neededAt;
        ExpiresAt = expiresAt;
        Note = note;
        Status = PlayerDemandStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Position { get; private set; } = string.Empty;
    public DateTimeOffset NeededAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string? Note { get; private set; }
    public PlayerDemandStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Team? Team { get; private set; }
    public User? CreatedByUser { get; private set; }

    public bool IsVisibleAt(DateTimeOffset now)
    {
        return Status == PlayerDemandStatus.Active && ExpiresAt > now;
    }

    public void MarkFilled()
    {
        Status = PlayerDemandStatus.Filled;
    }

    public void Cancel()
    {
        Status = PlayerDemandStatus.Cancelled;
    }
}
