using CPElite.Contracts.Common;

namespace CPElite.Contracts.Billing;

public sealed record PlayerSigningAccessCheckResponse(
    Guid TeamId,
    Guid UserId,
    bool CanJoinTeam,
    bool HasIndividualAccess,
    bool AlreadyUsesTeamSlot,
    bool WillConsumeTeamSlot,
    bool HasFreeTeamSlot,
    int FreeTeamSlots,
    TournamentAccessSource AccessSource,
    string Message);
