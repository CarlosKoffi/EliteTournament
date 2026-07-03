using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Team?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken = default);
    Task<Team?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
    Task<Team?> GetByEaClubIdAsync(long eaClubId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Team>> GetTeamsLinkedToEaAsync(CancellationToken cancellationToken = default);
    Task<TeamMember?> GetMembershipAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);
    Task<TeamMember?> GetActiveMembershipForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamMember>> GetMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamMember>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamJoinRequest?> GetPendingJoinRequestAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);
    Task<TeamJoinRequest?> GetJoinRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamJoinRequest>> GetPendingJoinRequestsAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamPosition>> GetPositionsAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamPosition?> GetPositionAsync(Guid teamId, Guid positionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamScheduleSlot>> GetScheduleSlotsAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamScheduleSlot?> GetScheduleSlotAsync(Guid teamId, Guid slotId, CancellationToken cancellationToken = default);
    Task<TeamPlayerDemand?> GetPlayerDemandAsync(Guid teamId, Guid demandId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamPlayerDemand>> GetActivePlayerDemandsAsync(DateTimeOffset startInclusive, DateTimeOffset endExclusive, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
    Task AddMemberAsync(TeamMember membership, CancellationToken cancellationToken = default);
    Task AddJoinRequestAsync(TeamJoinRequest joinRequest, CancellationToken cancellationToken = default);
    Task AddPositionAsync(TeamPosition position, CancellationToken cancellationToken = default);
    Task AddScheduleSlotAsync(TeamScheduleSlot slot, CancellationToken cancellationToken = default);
    Task AddPlayerDemandAsync(TeamPlayerDemand demand, CancellationToken cancellationToken = default);
    void RemovePosition(TeamPosition position);
    void RemoveScheduleSlot(TeamScheduleSlot slot);
}
