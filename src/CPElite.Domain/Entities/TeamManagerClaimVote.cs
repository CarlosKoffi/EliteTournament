namespace CPElite.Domain.Entities;

public sealed class TeamManagerClaimVote
{
    private TeamManagerClaimVote() { }

    public TeamManagerClaimVote(Guid id, Guid claimId, Guid teamId, Guid voterUserId, DateTimeOffset createdAt)
    {
        Id = id;
        ClaimId = claimId;
        TeamId = teamId;
        VoterUserId = voterUserId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid VoterUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public TeamManagerClaim? Claim { get; private set; }
    public User? VoterUser { get; private set; }
}
