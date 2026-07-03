using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class AccessRepository : IAccessRepository
{
    private readonly CPEliteDbContext _dbContext;

    public AccessRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserTournamentAccess?> GetIndividualAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserTournamentAccesses.FirstOrDefaultAsync(access => access.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamSlotPackage>> GetTeamSlotPackagesAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamSlotPackages
            .Where(package => package.TeamId == teamId)
            .OrderBy(package => package.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TeamSlotAssignment>> GetActiveTeamSlotAssignmentsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TeamSlotAssignments
            .Include(assignment => assignment.User)
            .Where(assignment => assignment.TeamId == teamId && assignment.ReleasedAt == null)
            .OrderBy(assignment => assignment.AssignedAt)
            .ToArrayAsync(cancellationToken);
    }

    public Task<TeamSlotAssignment?> GetActiveTeamSlotAssignmentAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TeamSlotAssignments
            .Include(assignment => assignment.User)
            .FirstOrDefaultAsync(assignment => assignment.TeamId == teamId && assignment.UserId == userId && assignment.ReleasedAt == null, cancellationToken);
    }

    public Task<TeamSlotAssignment?> GetAnyActiveSlotAssignmentForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TeamSlotAssignments
            .Include(assignment => assignment.Team)
            .FirstOrDefaultAsync(assignment => assignment.UserId == userId && assignment.ReleasedAt == null, cancellationToken);
    }

    public async Task AddIndividualAccessAsync(UserTournamentAccess access, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserTournamentAccesses.AddAsync(access, cancellationToken);
    }

    public async Task AddTeamSlotPackageAsync(TeamSlotPackage package, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamSlotPackages.AddAsync(package, cancellationToken);
    }

    public async Task AddTeamSlotAssignmentAsync(TeamSlotAssignment assignment, CancellationToken cancellationToken = default)
    {
        await _dbContext.TeamSlotAssignments.AddAsync(assignment, cancellationToken);
    }
}
