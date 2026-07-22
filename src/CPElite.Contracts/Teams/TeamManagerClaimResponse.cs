using CPElite.Contracts.Common;

namespace CPElite.Contracts.Teams;

public sealed record TeamManagerClaimResponse(
    Guid Id,
    Guid TeamId,
    Guid ClaimantUserId,
    string ClaimantDisplayName,
    string? ClaimantGamertag,
    TeamRole RequestedRole,
    TeamManagerClaimStatus Status,
    int ApprovalCount,
    int ApprovalThreshold,
    bool CurrentUserHasApproved,
    bool CanCurrentUserApprove,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);
