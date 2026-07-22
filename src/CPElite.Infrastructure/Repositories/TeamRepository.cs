using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class TeamRepository : ITeamRepository
{
    private readonly CPEliteDbContext _dbContext;

    public TeamRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Teams.FirstOrDefaultAsync(team => team.Id == id, cancellationToken);
    }

    public async Task<Team?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Teams.FirstOrDefaultAsync(team => team.InviteCode == inviteCode, cancellationToken);
    }

    public async Task<Team?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Teams.FirstOrDefaultAsync(team => team.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<Team?> GetByEaClubIdAsync(long eaClubId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Teams.FirstOrDefaultAsync(team => team.EaClubId == eaClubId && !team.IsArchived, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Team>> SearchByNameAsync(string normalizedSearch, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Teams
            .Where(team => !team.IsArchived && team.NormalizedName.Contains(normalizedSearch))
            .OrderBy(team => team.NormalizedName == normalizedSearch ? 0 : 1)
            .ThenBy(team => team.Name)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }
    public async Task<IReadOnlyCollection<Team>> GetTeamsLinkedToEaAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Teams
            .Where(team => team.EaClubId != null && !team.IsArchived)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TeamMember?> GetMembershipAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamMembers
            .Include(member => member.User)
            .Include(member => member.Team)
            .FirstOrDefaultAsync(member => member.TeamId == teamId && member.UserId == userId, cancellationToken);
    }

    public async Task<TeamMember?> GetActiveMembershipForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamMembers
            .Include(member => member.User)
            .Include(member => member.Team)
            .FirstOrDefaultAsync(member => member.UserId == userId && member.Status == Domain.Enums.MembershipStatus.Active && member.Team != null && !member.Team.IsArchived, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamMember>> GetMembershipsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamMembers
            .Include(member => member.User)
            .Include(member => member.Team)
            .Where(member => member.UserId == userId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamMember>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamMembers
            .Include(member => member.User)
            .Include(member => member.Team)
            .Where(member => member.TeamId == teamId)
            .OrderBy(member => member.JoinedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TeamManagerClaim?> GetManagerClaimAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamManagerClaims
            .Include(claim => claim.ClaimantUser)
            .Include(claim => claim.Votes)
            .FirstOrDefaultAsync(claim => claim.Id == claimId, cancellationToken);
    }

    public async Task<TeamManagerClaim?> GetPendingManagerClaimAsync(Guid teamId, Guid claimantUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamManagerClaims
            .Include(claim => claim.ClaimantUser)
            .Include(claim => claim.Votes)
            .FirstOrDefaultAsync(claim => claim.TeamId == teamId && claim.ClaimantUserId == claimantUserId && claim.Status == Domain.Enums.TeamManagerClaimStatus.Pending, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamManagerClaim>> GetManagerClaimsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamManagerClaims
            .Include(claim => claim.ClaimantUser)
            .Include(claim => claim.Votes)
            .Where(claim => claim.TeamId == teamId && claim.Status == Domain.Enums.TeamManagerClaimStatus.Pending)
            .OrderBy(claim => claim.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TeamManagerClaimVote?> GetManagerClaimVoteAsync(Guid claimId, Guid voterUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamManagerClaimVotes
            .FirstOrDefaultAsync(vote => vote.ClaimId == claimId && vote.VoterUserId == voterUserId, cancellationToken);
    }

    public async Task<TeamJoinRequest?> GetPendingJoinRequestAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamJoinRequests
            .Include(joinRequest => joinRequest.User)
            .FirstOrDefaultAsync(joinRequest => joinRequest.TeamId == teamId && joinRequest.UserId == userId && joinRequest.Status == Domain.Enums.JoinRequestStatus.Pending, cancellationToken);
    }

    public async Task<TeamJoinRequest?> GetJoinRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamJoinRequests
            .Include(joinRequest => joinRequest.User)
            .Include(joinRequest => joinRequest.Team)
            .FirstOrDefaultAsync(joinRequest => joinRequest.Id == requestId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamJoinRequest>> GetPendingJoinRequestsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamJoinRequests
            .Include(joinRequest => joinRequest.User)
            .Where(joinRequest => joinRequest.TeamId == teamId && joinRequest.Status == Domain.Enums.JoinRequestStatus.Pending)
            .OrderBy(joinRequest => joinRequest.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamPosition>> GetPositionsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamPositions
            .Where(position => position.TeamId == teamId)
            .OrderBy(position => position.SortOrder)
            .ThenBy(position => position.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TeamPosition?> GetPositionAsync(Guid teamId, Guid positionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamPositions.FirstOrDefaultAsync(position => position.TeamId == teamId && position.Id == positionId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamScheduleSlot>> GetScheduleSlotsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamScheduleSlots
            .Where(slot => slot.TeamId == teamId)
            .OrderBy(slot => slot.DayOfWeek)
            .ThenBy(slot => slot.StartTime)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TeamScheduleSlot?> GetScheduleSlotAsync(Guid teamId, Guid slotId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamScheduleSlots.FirstOrDefaultAsync(slot => slot.TeamId == teamId && slot.Id == slotId, cancellationToken);
    }

    public async Task<TeamPlayerDemand?> GetPlayerDemandAsync(Guid teamId, Guid demandId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamPlayerDemands
            .Include(demand => demand.Team)
            .FirstOrDefaultAsync(demand => demand.TeamId == teamId && demand.Id == demandId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamPlayerDemand>> GetActivePlayerDemandsAsync(DateTimeOffset startInclusive, DateTimeOffset endExclusive, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamPlayerDemands
            .Include(demand => demand.Team)
            .Where(demand => demand.Status == Domain.Enums.PlayerDemandStatus.Active && demand.ExpiresAt > now && demand.NeededAt >= startInclusive && demand.NeededAt < endExclusive && demand.Team != null && !demand.Team.IsArchived)
            .OrderBy(demand => demand.NeededAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
    {
        await _dbContext.Teams.AddAsync(team, cancellationToken);
    }

    public async Task AddMemberAsync(TeamMember membership, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamMembers.AddAsync(membership, cancellationToken);
    }

    public async Task AddManagerClaimAsync(TeamManagerClaim claim, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamManagerClaims.AddAsync(claim, cancellationToken);
    }

    public async Task AddManagerClaimVoteAsync(TeamManagerClaimVote vote, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamManagerClaimVotes.AddAsync(vote, cancellationToken);
    }

    public async Task AddJoinRequestAsync(TeamJoinRequest joinRequest, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamJoinRequests.AddAsync(joinRequest, cancellationToken);
    }

    public async Task AddPositionAsync(TeamPosition position, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamPositions.AddAsync(position, cancellationToken);
    }

    public async Task AddScheduleSlotAsync(TeamScheduleSlot slot, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamScheduleSlots.AddAsync(slot, cancellationToken);
    }

    public async Task AddPlayerDemandAsync(TeamPlayerDemand demand, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamPlayerDemands.AddAsync(demand, cancellationToken);
    }

    public void RemovePosition(TeamPosition position)
    {
        _dbContext.TeamPositions.Remove(position);
    }

    public void RemoveScheduleSlot(TeamScheduleSlot slot)
    {
        _dbContext.TeamScheduleSlots.Remove(slot);
    }
}
