using CPElite.Application.Abstractions;
using CPElite.Contracts.Billing;
using CPElite.Contracts.Common;
using CPElite.Domain.Entities;
using DomainAccessProvider = CPElite.Domain.Enums.AccessProvider;
using DomainTeamRole = CPElite.Domain.Enums.TeamRole;

namespace CPElite.Application.Services;

public sealed class AccessService
{
    private readonly IAccessRepository _access;
    private readonly ITeamRepository _teams;
    private readonly IUserRepository _users;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public AccessService(IAccessRepository access, ITeamRepository teams, IUserRepository users, IClock clock, IUnitOfWork unitOfWork)
    {
        _access = access;
        _teams = teams;
        _users = users;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlayerTournamentAccessResponse>> PurchaseIndividualAccessAsync(Guid userId, PurchaseIndividualAccessRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderTransactionId))
        {
            return Result<PlayerTournamentAccessResponse>.Failure(ErrorType.Validation, "access.transaction_required", "Payment transaction id is required.");
        }

        var existing = await _access.GetIndividualAccessAsync(userId, cancellationToken);
        if (existing is null)
        {
            await _access.AddIndividualAccessAsync(new UserTournamentAccess(Guid.NewGuid(), userId, (DomainAccessProvider)(int)request.Provider, request.ProviderTransactionId.Trim(), _clock.UtcNow), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<PlayerTournamentAccessResponse>.Success(new PlayerTournamentAccessResponse(userId, true, TournamentAccessSource.Individual, null, null));
    }

    public async Task<Result<TeamAccessSummaryResponse>> PurchaseTeamSlotsAsync(Guid actorUserId, Guid teamId, PurchaseTeamSlotsRequest request, CancellationToken cancellationToken = default)
    {
        var auth = await EnsureCanManageTeamAccessAsync(actorUserId, teamId, cancellationToken);
        if (!auth.IsSuccess)
        {
            return Result<TeamAccessSummaryResponse>.Failure(auth.Error!.Type, auth.Error.Code, auth.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(request.ProviderTransactionId))
        {
            return Result<TeamAccessSummaryResponse>.Failure(ErrorType.Validation, "access.transaction_required", "Payment transaction id is required.");
        }

        var slots = (int)request.PackageSize;
        var package = new TeamSlotPackage(Guid.NewGuid(), teamId, actorUserId, slots, CalculateSlotPrice(slots), "EUR", (DomainAccessProvider)(int)request.Provider, request.ProviderTransactionId.Trim(), _clock.UtcNow);
        await _access.AddTeamSlotPackageAsync(package, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetTeamAccessSummaryAsync(actorUserId, teamId, cancellationToken);
    }

    public async Task<Result<TeamSlotAssignmentResponse>> AssignTeamSlotAsync(Guid actorUserId, Guid teamId, AssignTeamSlotRequest request, CancellationToken cancellationToken = default)
    {
        var auth = await EnsureCanManageTeamAccessAsync(actorUserId, teamId, cancellationToken);
        if (!auth.IsSuccess)
        {
            return Result<TeamSlotAssignmentResponse>.Failure(auth.Error!.Type, auth.Error.Code, auth.Error.Message);
        }

        var target = await _teams.GetMembershipAsync(teamId, request.UserId, cancellationToken);
        if (target is null || !target.IsActive)
        {
            return Result<TeamSlotAssignmentResponse>.Failure(ErrorType.NotFound, "access.member_not_found", "Player must be an active member of the team.");
        }

        if (await _access.GetIndividualAccessAsync(request.UserId, cancellationToken) is not null)
        {
            return Result<TeamSlotAssignmentResponse>.Success(new TeamSlotAssignmentResponse(null, teamId, request.UserId, false, "Player already owns individual tournament access; no club slot consumed."));
        }

        var existing = await _access.GetActiveTeamSlotAssignmentAsync(teamId, request.UserId, cancellationToken);
        if (existing is not null)
        {
            return Result<TeamSlotAssignmentResponse>.Success(new TeamSlotAssignmentResponse(existing.Id, teamId, request.UserId, true, "Player already uses a club slot."));
        }

        var packages = await _access.GetTeamSlotPackagesAsync(teamId, cancellationToken);
        var assignments = await _access.GetActiveTeamSlotAssignmentsAsync(teamId, cancellationToken);
        var availablePackage = packages.FirstOrDefault(package => assignments.Count(assignment => assignment.TeamSlotPackageId == package.Id) < package.SlotCount);
        if (availablePackage is null)
        {
            return Result<TeamSlotAssignmentResponse>.Failure(ErrorType.Conflict, "access.no_free_slot", "No free team slot is available.");
        }

        var assignment = new TeamSlotAssignment(Guid.NewGuid(), availablePackage.Id, teamId, request.UserId, actorUserId, _clock.UtcNow);
        await _access.AddTeamSlotAssignmentAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TeamSlotAssignmentResponse>.Success(new TeamSlotAssignmentResponse(assignment.Id, teamId, request.UserId, true, "Team slot assigned."));
    }

    public async Task<Result<PlayerSigningAccessCheckResponse>> CheckPlayerSigningAccessAsync(Guid actorUserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var auth = await EnsureCanManageTeamAccessAsync(actorUserId, teamId, cancellationToken);
        if (!auth.IsSuccess)
        {
            return Result<PlayerSigningAccessCheckResponse>.Failure(auth.Error!.Type, auth.Error.Code, auth.Error.Message);
        }

        var activeMembership = await _teams.GetActiveMembershipForUserAsync(userId, cancellationToken);
        if (activeMembership is not null && activeMembership.TeamId != teamId)
        {
            return Result<PlayerSigningAccessCheckResponse>.Success(new PlayerSigningAccessCheckResponse(
                teamId,
                userId,
                false,
                false,
                false,
                false,
                false,
                0,
                TournamentAccessSource.None,
                $"Player already belongs to {activeMembership.Team?.Name ?? "another team"}."
            ));
        }

        var hasIndividualAccess = await _access.GetIndividualAccessAsync(userId, cancellationToken) is not null;
        var existingTeamSlot = await _access.GetActiveTeamSlotAssignmentAsync(teamId, userId, cancellationToken);
        var packages = await _access.GetTeamSlotPackagesAsync(teamId, cancellationToken);
        var assignments = await _access.GetActiveTeamSlotAssignmentsAsync(teamId, cancellationToken);
        var freeSlots = Math.Max(0, packages.Sum(package => package.SlotCount) - assignments.Count);

        if (hasIndividualAccess)
        {
            return Result<PlayerSigningAccessCheckResponse>.Success(new PlayerSigningAccessCheckResponse(teamId, userId, true, true, existingTeamSlot is not null, false, freeSlots > 0, freeSlots, TournamentAccessSource.Individual, "Player owns individual tournament access; no club slot is needed."));
        }

        if (existingTeamSlot is not null)
        {
            return Result<PlayerSigningAccessCheckResponse>.Success(new PlayerSigningAccessCheckResponse(teamId, userId, true, false, true, false, freeSlots > 0, freeSlots, TournamentAccessSource.TeamSlot, "Player already uses one of this club's slots."));
        }

        return Result<PlayerSigningAccessCheckResponse>.Success(new PlayerSigningAccessCheckResponse(
            teamId,
            userId,
            freeSlots > 0,
            false,
            false,
            freeSlots > 0,
            freeSlots > 0,
            freeSlots,
            freeSlots > 0 ? TournamentAccessSource.TeamSlot : TournamentAccessSource.None,
            freeSlots > 0 ? "Player has no individual access and will consume one club slot." : "Player has no individual access and the club has no free slot."
        ));
    }

    public async Task<Result<TeamAccessSummaryResponse>> GetTeamAccessSummaryAsync(Guid actorUserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var auth = await EnsureCanManageTeamAccessAsync(actorUserId, teamId, cancellationToken);
        if (!auth.IsSuccess)
        {
            return Result<TeamAccessSummaryResponse>.Failure(auth.Error!.Type, auth.Error.Code, auth.Error.Message);
        }

        var packages = await _access.GetTeamSlotPackagesAsync(teamId, cancellationToken);
        var assignments = await _access.GetActiveTeamSlotAssignmentsAsync(teamId, cancellationToken);
        var holders = assignments.Select(assignment =>
        {
            var user = assignment.User ?? throw new InvalidOperationException("Slot assignment user was not loaded.");
            return new TeamSlotHolderResponse(user.Id, user.DisplayName, user.Gamertag, assignment.AssignedAt);
        }).ToArray();

        var totalSlots = packages.Sum(package => package.SlotCount);
        return Result<TeamAccessSummaryResponse>.Success(new TeamAccessSummaryResponse(auth.Value!.Team.Id, auth.Value.Team.Name, totalSlots, assignments.Count, Math.Max(0, totalSlots - assignments.Count), packages.Sum(package => package.PricePaid), holders));
    }

    public async Task ReleaseTeamSlotIfAnyAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var assignment = await _access.GetActiveTeamSlotAssignmentAsync(teamId, userId, cancellationToken);
        if (assignment is not null)
        {
            assignment.Release(_clock.UtcNow);
        }
    }

    private async Task<Result<TeamAuthorization>> EnsureCanManageTeamAccessAsync(Guid actorUserId, Guid teamId, CancellationToken cancellationToken)
    {
        var membership = await _teams.GetMembershipAsync(teamId, actorUserId, cancellationToken);
        if (membership is null || !membership.CanManageTeamAccess())
        {
            return Result<TeamAuthorization>.Failure(ErrorType.Forbidden, "access.manage_forbidden", "Only owner or manager can manage team slots.");
        }

        var team = membership.Team ?? await _teams.GetByIdAsync(teamId, cancellationToken);
        return team is null || team.IsArchived
            ? Result<TeamAuthorization>.Failure(ErrorType.NotFound, "team.not_found", "Team was not found.")
            : Result<TeamAuthorization>.Success(new TeamAuthorization(team, membership));
    }

    public static decimal CalculateSlotPrice(int slots) => slots * 2.5m;

    private sealed record TeamAuthorization(Team Team, TeamMember Membership);
}
