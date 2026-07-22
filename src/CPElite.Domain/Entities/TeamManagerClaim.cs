using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TeamManagerClaim
{
    private TeamManagerClaim() { }

    public TeamManagerClaim(Guid id, Guid teamId, Guid claimantUserId, TeamRole requestedRole, int approvalThreshold, DateTimeOffset createdAt)
    {
        Id = id;
        TeamId = teamId;
        ClaimantUserId = claimantUserId;
        RequestedRole = requestedRole;
        ApprovalThreshold = approvalThreshold;
        Status = TeamManagerClaimStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid ClaimantUserId { get; private set; }
    public TeamRole RequestedRole { get; private set; }
    public int ApprovalThreshold { get; private set; }
    public TeamManagerClaimStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public Team? Team { get; private set; }
    public User? ClaimantUser { get; private set; }
    public IReadOnlyCollection<TeamManagerClaimVote> Votes => _votes;
    private readonly List<TeamManagerClaimVote> _votes = new();

    public void Approve(DateTimeOffset resolvedAt)
    {
        Status = TeamManagerClaimStatus.Approved;
        ResolvedAt = resolvedAt;
    }

    public void Cancel(DateTimeOffset resolvedAt)
    {
        Status = TeamManagerClaimStatus.Cancelled;
        ResolvedAt = resolvedAt;
    }
}
