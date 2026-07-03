using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface IAccessRepository
{
    Task<UserTournamentAccess?> GetIndividualAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamSlotPackage>> GetTeamSlotPackagesAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamSlotAssignment>> GetActiveTeamSlotAssignmentsAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamSlotAssignment?> GetActiveTeamSlotAssignmentAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);
    Task<TeamSlotAssignment?> GetAnyActiveSlotAssignmentForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddIndividualAccessAsync(UserTournamentAccess access, CancellationToken cancellationToken = default);
    Task AddTeamSlotPackageAsync(TeamSlotPackage package, CancellationToken cancellationToken = default);
    Task AddTeamSlotAssignmentAsync(TeamSlotAssignment assignment, CancellationToken cancellationToken = default);
}
