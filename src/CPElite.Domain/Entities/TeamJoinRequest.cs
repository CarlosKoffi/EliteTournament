using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TeamJoinRequest
{
    private TeamJoinRequest() { }

    public TeamJoinRequest(Guid id, Guid teamId, Guid userId, string? message, DateTimeOffset createdAt)
    {
        Id = id;
        TeamId = teamId;
        UserId = userId;
        Message = message;
        Status = JoinRequestStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public string? Message { get; private set; }
    public JoinRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public Guid? DecidedByUserId { get; private set; }
    public Team? Team { get; private set; }
    public User? User { get; private set; }

    public void Approve(Guid decidedByUserId, DateTimeOffset decidedAt)
    {
        Status = JoinRequestStatus.Approved;
        DecidedByUserId = decidedByUserId;
        DecidedAt = decidedAt;
    }

    public void Reject(Guid decidedByUserId, DateTimeOffset decidedAt)
    {
        Status = JoinRequestStatus.Rejected;
        DecidedByUserId = decidedByUserId;
        DecidedAt = decidedAt;
    }
}
