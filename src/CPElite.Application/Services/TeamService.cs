using CPElite.Application.Abstractions;
using CPElite.Contracts.Common;
using CPElite.Contracts.Teams;
using CPElite.Domain.Entities;
using DomainPlatform = CPElite.Domain.Enums.Platform;
using DomainTeamRole = CPElite.Domain.Enums.TeamRole;

namespace CPElite.Application.Services;

public sealed class TeamService
{
    private readonly IUserRepository _users;
    private readonly ITeamRepository _teams;
    private readonly AccessService _accessService;
    private readonly EaSyncService _eaSyncService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public TeamService(IUserRepository users, ITeamRepository teams, AccessService accessService, EaSyncService eaSyncService, IClock clock, IUnitOfWork unitOfWork)
    {
        _users = users;
        _teams = teams;
        _accessService = accessService;
        _eaSyncService = eaSyncService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TeamResponse>> CreateTeamAsync(Guid userId, CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<TeamResponse>.Failure(ErrorType.Validation, "team.name_required", "Team name is required.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<TeamResponse>.Failure(ErrorType.NotFound, "user.not_found", "User was not found.");
        }

        var normalizedName = Normalize(request.Name);
        if (await _teams.GetByNormalizedNameAsync(normalizedName, cancellationToken) is not null)
        {
            return Result<TeamResponse>.Failure(ErrorType.Conflict, "team.name_taken", "A team already exists with this name.");
        }

        var team = new Team(Guid.NewGuid(), request.Name.Trim(), normalizedName, NormalizeOptional(request.ShortName), (DomainPlatform)(int)request.Platform, NormalizeOptional(request.Region), CreateInviteCode(), userId, _clock.UtcNow);
        team.UpdateProfile(team.Name, team.NormalizedName, team.ShortName, team.Platform, team.Region, NormalizeOptional(request.Description), request.EaClubId, NormalizeOptional(request.LogoUrl), NormalizeOptional(request.BannerUrl));
        team.AddOwner(userId, _clock.UtcNow);

        await _teams.AddAsync(team, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (team.EaClubId is not null)
        {
            try
            {
                await _eaSyncService.SyncTeamAsync(team.Id, forceRefresh: true, cancellationToken);
            }
            catch
            {
                // Team creation should not fail because the external EA data source is temporarily unavailable.
            }
        }

        var ownerMembership = await _teams.GetMembershipAsync(team.Id, userId, cancellationToken);
        return Result<TeamResponse>.Success(ToTeamResponse(team, ownerMembership!));
    }

    public async Task<Result<TeamResponse>> JoinTeamAsync(Guid userId, JoinTeamRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<TeamResponse>.Failure(ErrorType.NotFound, "user.not_found", "User was not found.");
        }

        var team = await _teams.GetByInviteCodeAsync(request.InviteCode.Trim(), cancellationToken);
        if (team is null || !team.IsInviteActive(_clock.UtcNow))
        {
            return Result<TeamResponse>.Failure(ErrorType.NotFound, "team.invite_not_found", "Invite code is invalid.");
        }

        var existingMembership = await _teams.GetMembershipAsync(team.Id, userId, cancellationToken);
        if (existingMembership is not null)
        {
            return Result<TeamResponse>.Failure(ErrorType.Conflict, "team.already_joined", "Player is already a member of this team.");
        }

        if (await _teams.GetActiveMembershipForUserAsync(userId, cancellationToken) is not null)
        {
            return Result<TeamResponse>.Failure(ErrorType.Conflict, "team.user_already_in_team", "A player can only belong to one active team.");
        }

        var membership = TeamMember.Create(team.Id, userId, DomainTeamRole.Player, _clock.UtcNow);
        await _teams.AddMemberAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedMembership = await _teams.GetMembershipAsync(team.Id, userId, cancellationToken);
        return Result<TeamResponse>.Success(ToTeamResponse(team, savedMembership!));
    }

    public async Task<Result<IReadOnlyCollection<TeamMemberResponse>>> GetMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var members = await _teams.GetTeamMembersAsync(teamId, cancellationToken);
        return Result<IReadOnlyCollection<TeamMemberResponse>>.Success(members.Select(Mapping.ToMemberResponse).ToArray());
    }

    public async Task<Result<TeamResponse>> GetTeamAsync(Guid actorUserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var membership = await _teams.GetMembershipAsync(teamId, actorUserId, cancellationToken);
        if (membership is null)
        {
            return Result<TeamResponse>.Failure(ErrorType.Forbidden, "team.profile_forbidden", "Only team members can view this team profile.");
        }

        var team = membership.Team ?? await _teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null || team.IsArchived)
        {
            return Result<TeamResponse>.Failure(ErrorType.NotFound, "team.not_found", "Team was not found.");
        }

        return Result<TeamResponse>.Success(ToTeamResponse(team, membership));
    }

    public async Task<Result<TeamMemberResponse>> ChangeRoleAsync(Guid actorUserId, Guid teamId, Guid targetUserId, TeamRole role, CancellationToken cancellationToken = default)
    {
        var actor = await _teams.GetMembershipAsync(teamId, actorUserId, cancellationToken);
        if (actor is null || !actor.CanManageRoles())
        {
            return Result<TeamMemberResponse>.Failure(ErrorType.Forbidden, "team.role_forbidden", "Only team owners can change member roles.");
        }

        var target = await _teams.GetMembershipAsync(teamId, targetUserId, cancellationToken);
        if (target is null)
        {
            return Result<TeamMemberResponse>.Failure(ErrorType.NotFound, "team.member_not_found", "Team member was not found.");
        }

        var domainRole = (DomainTeamRole)(int)role;
        if (target.Role == DomainTeamRole.Owner && domainRole != DomainTeamRole.Owner)
        {
            var owners = (await _teams.GetTeamMembersAsync(teamId, cancellationToken)).Count(member => member.Role == DomainTeamRole.Owner && member.IsActive);
            if (owners <= 1)
            {
                return Result<TeamMemberResponse>.Failure(ErrorType.Conflict, "team.last_owner", "A team must keep at least one owner.");
            }
        }

        target.ChangeRole(domainRole);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TeamMemberResponse>.Success(Mapping.ToMemberResponse(target));
    }

    public async Task<Result<TeamMemberResponse>> RemoveMemberAsync(Guid actorUserId, Guid teamId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _teams.GetMembershipAsync(teamId, actorUserId, cancellationToken);
        if (actor is null || !actor.CanManageRoles())
        {
            return Result<TeamMemberResponse>.Failure(ErrorType.Forbidden, "team.remove_forbidden", "Only team owners can remove a member.");
        }

        var target = await _teams.GetMembershipAsync(teamId, targetUserId, cancellationToken);
        if (target is null || !target.IsActive)
        {
            return Result<TeamMemberResponse>.Failure(ErrorType.NotFound, "team.member_not_found", "Team member was not found.");
        }

        if (target.Role == DomainTeamRole.Owner)
        {
            var owners = (await _teams.GetTeamMembersAsync(teamId, cancellationToken)).Count(member => member.Role == DomainTeamRole.Owner && member.IsActive);
            if (owners <= 1)
            {
                return Result<TeamMemberResponse>.Failure(ErrorType.Conflict, "team.last_owner", "A team must keep at least one owner.");
            }
        }

        target.Remove();
        await _accessService.ReleaseTeamSlotIfAnyAsync(teamId, targetUserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TeamMemberResponse>.Success(Mapping.ToMemberResponse(target));
    }

    public async Task<Result<TeamResponse>> UpdateProfileAsync(Guid actorUserId, Guid teamId, UpdateTeamProfileRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<TeamResponse>.Failure(ErrorType.Validation, "team.name_required", "Team name is required.");
        }

        var team = authorization.Value!.Team;
        var normalizedName = Normalize(request.Name);
        var existing = await _teams.GetByNormalizedNameAsync(normalizedName, cancellationToken);
        if (existing is not null && existing.Id != teamId)
        {
            return Result<TeamResponse>.Failure(ErrorType.Conflict, "team.name_taken", "A team already exists with this name.");
        }

        team.UpdateProfile(request.Name.Trim(), normalizedName, NormalizeOptional(request.ShortName), (DomainPlatform)(int)request.Platform, NormalizeOptional(request.Region), NormalizeOptional(request.Description), request.EaClubId, NormalizeOptional(request.LogoUrl), NormalizeOptional(request.BannerUrl));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TeamResponse>.Success(ToTeamResponse(team, authorization.Value.Membership));
    }

    public async Task<Result<TeamResponse>> UpdateSocialLinksAsync(Guid actorUserId, Guid teamId, UpdateTeamSocialLinksRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var team = authorization.Value!.Team;
        team.UpdateSocialLinks(NormalizeOptional(request.DiscordUrl), NormalizeOptional(request.TwitchUrl), NormalizeOptional(request.TikTokUrl), NormalizeOptional(request.TwitterUrl));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TeamResponse>.Success(ToTeamResponse(team, authorization.Value.Membership));
    }

    public async Task<Result<TeamResponse>> UpdateSettingsAsync(Guid actorUserId, Guid teamId, UpdateTeamSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var team = authorization.Value!.Team;
        team.UpdateSettings(request.RequireJoinApproval);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TeamResponse>.Success(ToTeamResponse(team, authorization.Value.Membership));
    }

    public async Task<Result<TeamResponse>> ExpireInviteCodeAsync(Guid actorUserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        authorization.Value!.Team.ExpireInviteCode(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TeamResponse>.Success(ToTeamResponse(authorization.Value.Team, authorization.Value.Membership));
    }

    public async Task<Result<TeamResponse>> RegenerateInviteCodeAsync(Guid actorUserId, Guid teamId, RegenerateInviteCodeRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        authorization.Value!.Team.RegenerateInviteCode(CreateInviteCode(), request.ExpiresAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TeamResponse>.Success(ToTeamResponse(authorization.Value.Team, authorization.Value.Membership));
    }

    public async Task<Result<TeamResponse>> ArchiveTeamAsync(Guid actorUserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        authorization.Value!.Team.Archive(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TeamResponse>.Success(ToTeamResponse(authorization.Value.Team, authorization.Value.Membership));
    }

    public async Task<Result<TeamJoinRequestResponse>> CreateJoinRequestAsync(Guid userId, Guid teamId, CreateJoinRequestRequest request, CancellationToken cancellationToken = default)
    {
        var team = await _teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null || team.IsArchived)
        {
            return Result<TeamJoinRequestResponse>.Failure(ErrorType.NotFound, "team.not_found", "Team was not found.");
        }

        if (await _teams.GetMembershipAsync(teamId, userId, cancellationToken) is not null)
        {
            return Result<TeamJoinRequestResponse>.Failure(ErrorType.Conflict, "team.already_joined", "Player is already a member of this team.");
        }

        if (await _teams.GetActiveMembershipForUserAsync(userId, cancellationToken) is not null)
        {
            return Result<TeamJoinRequestResponse>.Failure(ErrorType.Conflict, "team.user_already_in_team", "A player can only belong to one active team.");
        }

        if (await _teams.GetPendingJoinRequestAsync(teamId, userId, cancellationToken) is not null)
        {
            return Result<TeamJoinRequestResponse>.Failure(ErrorType.Conflict, "team.join_request_pending", "Player already has a pending request for this team.");
        }

        var joinRequest = new TeamJoinRequest(Guid.NewGuid(), teamId, userId, NormalizeOptional(request.Message), _clock.UtcNow);
        await _teams.AddJoinRequestAsync(joinRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _teams.GetJoinRequestAsync(joinRequest.Id, cancellationToken);
        return Result<TeamJoinRequestResponse>.Success(ToJoinRequestResponse(saved!));
    }

    public async Task<Result<IReadOnlyCollection<TeamJoinRequestResponse>>> GetPendingJoinRequestsAsync(Guid actorUserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<IReadOnlyCollection<TeamJoinRequestResponse>>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var requests = await _teams.GetPendingJoinRequestsAsync(teamId, cancellationToken);
        return Result<IReadOnlyCollection<TeamJoinRequestResponse>>.Success(requests.Select(ToJoinRequestResponse).ToArray());
    }

    public async Task<Result<TeamMemberResponse>> ApproveJoinRequestAsync(Guid actorUserId, Guid teamId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamMemberResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var joinRequest = await _teams.GetJoinRequestAsync(requestId, cancellationToken);
        if (joinRequest is null || joinRequest.TeamId != teamId || joinRequest.Status != Domain.Enums.JoinRequestStatus.Pending)
        {
            return Result<TeamMemberResponse>.Failure(ErrorType.NotFound, "team.join_request_not_found", "Join request was not found.");
        }

        if (await _teams.GetMembershipAsync(teamId, joinRequest.UserId, cancellationToken) is not null)
        {
            return Result<TeamMemberResponse>.Failure(ErrorType.Conflict, "team.already_joined", "Player is already a member of this team.");
        }

        if (await _teams.GetActiveMembershipForUserAsync(joinRequest.UserId, cancellationToken) is not null)
        {
            return Result<TeamMemberResponse>.Failure(ErrorType.Conflict, "team.user_already_in_team", "A player can only belong to one active team.");
        }

        joinRequest.Approve(actorUserId, _clock.UtcNow);
        var membership = TeamMember.Create(teamId, joinRequest.UserId, DomainTeamRole.Player, _clock.UtcNow);
        await _teams.AddMemberAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedMembership = await _teams.GetMembershipAsync(teamId, joinRequest.UserId, cancellationToken);
        return Result<TeamMemberResponse>.Success(Mapping.ToMemberResponse(savedMembership!));
    }

    public async Task<Result<TeamJoinRequestResponse>> RejectJoinRequestAsync(Guid actorUserId, Guid teamId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamJoinRequestResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var joinRequest = await _teams.GetJoinRequestAsync(requestId, cancellationToken);
        if (joinRequest is null || joinRequest.TeamId != teamId || joinRequest.Status != Domain.Enums.JoinRequestStatus.Pending)
        {
            return Result<TeamJoinRequestResponse>.Failure(ErrorType.NotFound, "team.join_request_not_found", "Join request was not found.");
        }

        joinRequest.Reject(actorUserId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TeamJoinRequestResponse>.Success(ToJoinRequestResponse(joinRequest));
    }

    public async Task<Result<TeamPositionResponse>> CreatePositionAsync(Guid actorUserId, Guid teamId, CreateTeamPositionRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamPositionResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<TeamPositionResponse>.Failure(ErrorType.Validation, "team.position_name_required", "Position name is required.");
        }

        var position = new TeamPosition(Guid.NewGuid(), teamId, request.Name.Trim(), NormalizeOptional(request.Description), request.SortOrder, _clock.UtcNow);
        await _teams.AddPositionAsync(position, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TeamPositionResponse>.Success(ToPositionResponse(position));
    }

    public async Task<Result<IReadOnlyCollection<TeamPositionResponse>>> GetPositionsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var positions = await _teams.GetPositionsAsync(teamId, cancellationToken);
        return Result<IReadOnlyCollection<TeamPositionResponse>>.Success(positions.Select(ToPositionResponse).ToArray());
    }

    public async Task<Result<bool>> DeletePositionAsync(Guid actorUserId, Guid teamId, Guid positionId, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<bool>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var position = await _teams.GetPositionAsync(teamId, positionId, cancellationToken);
        if (position is null)
        {
            return Result<bool>.Failure(ErrorType.NotFound, "team.position_not_found", "Position was not found.");
        }

        _teams.RemovePosition(position);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<TeamScheduleSlotResponse>> CreateScheduleSlotAsync(Guid actorUserId, Guid teamId, CreateTeamScheduleSlotRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<TeamScheduleSlotResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        if (request.EndTime <= request.StartTime)
        {
            return Result<TeamScheduleSlotResponse>.Failure(ErrorType.Validation, "team.schedule_invalid_time", "Schedule end time must be after start time.");
        }

        var slot = new TeamScheduleSlot(Guid.NewGuid(), teamId, request.DayOfWeek, request.StartTime, request.EndTime, NormalizeOptional(request.Label), _clock.UtcNow);
        await _teams.AddScheduleSlotAsync(slot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TeamScheduleSlotResponse>.Success(ToScheduleSlotResponse(slot));
    }

    public async Task<Result<IReadOnlyCollection<TeamScheduleSlotResponse>>> GetScheduleSlotsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var slots = await _teams.GetScheduleSlotsAsync(teamId, cancellationToken);
        return Result<IReadOnlyCollection<TeamScheduleSlotResponse>>.Success(slots.Select(ToScheduleSlotResponse).ToArray());
    }

    public async Task<Result<bool>> DeleteScheduleSlotAsync(Guid actorUserId, Guid teamId, Guid slotId, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<bool>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var slot = await _teams.GetScheduleSlotAsync(teamId, slotId, cancellationToken);
        if (slot is null)
        {
            return Result<bool>.Failure(ErrorType.NotFound, "team.schedule_slot_not_found", "Schedule slot was not found.");
        }

        _teams.RemoveScheduleSlot(slot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<PlayerDemandResponse>> CreatePlayerDemandAsync(Guid actorUserId, Guid teamId, CreatePlayerDemandRequest request, CancellationToken cancellationToken = default)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<PlayerDemandResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(request.Position))
        {
            return Result<PlayerDemandResponse>.Failure(ErrorType.Validation, "team.player_demand_position_required", "Position is required.");
        }

        var neededAt = request.NeededAt.ToUniversalTime();
        if (neededAt <= _clock.UtcNow)
        {
            return Result<PlayerDemandResponse>.Failure(ErrorType.Validation, "team.player_demand_time_in_past", "Needed time must be in the future.");
        }

        var expiresAt = (request.ExpiresAt ?? neededAt.AddHours(2)).ToUniversalTime();
        if (expiresAt <= _clock.UtcNow || expiresAt > neededAt.AddHours(6))
        {
            return Result<PlayerDemandResponse>.Failure(ErrorType.Validation, "team.player_demand_invalid_expiry", "Demand expiry must be after now and close to the needed time.");
        }

        var demand = new TeamPlayerDemand(Guid.NewGuid(), teamId, actorUserId, request.Position.Trim(), neededAt, expiresAt, NormalizeOptional(request.Note), _clock.UtcNow);
        await _teams.AddPlayerDemandAsync(demand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _teams.GetPlayerDemandAsync(teamId, demand.Id, cancellationToken);
        return Result<PlayerDemandResponse>.Success(TodayService.ToPlayerDemandResponse(saved!));
    }

    public async Task<Result<PlayerDemandResponse>> MarkPlayerDemandFilledAsync(Guid actorUserId, Guid teamId, Guid demandId, CancellationToken cancellationToken = default)
    {
        var result = await UpdatePlayerDemandStatusAsync(actorUserId, teamId, demandId, demand => demand.MarkFilled(), cancellationToken);
        return result;
    }

    public async Task<Result<PlayerDemandResponse>> CancelPlayerDemandAsync(Guid actorUserId, Guid teamId, Guid demandId, CancellationToken cancellationToken = default)
    {
        var result = await UpdatePlayerDemandStatusAsync(actorUserId, teamId, demandId, demand => demand.Cancel(), cancellationToken);
        return result;
    }

    private static TeamResponse ToTeamResponse(Team team, TeamMember membership)
    {
        return new TeamResponse(
            team.Id,
            team.Name,
            team.ShortName,
            (Platform)(int)team.Platform,
            team.Region,
            team.Description,
            team.EaClubId,
            team.LogoUrl,
            team.BannerUrl,
            team.DiscordUrl,
            team.TwitchUrl,
            team.TikTokUrl,
            team.TwitterUrl,
            team.RequireJoinApproval,
            team.IsArchived,
            team.InviteCode,
            team.InviteCodeExpiresAt,
            Mapping.ToMemberResponse(membership));
    }

    private async Task<Result<TeamAuthorization>> EnsureCanManageTeamAsync(Guid actorUserId, Guid teamId, CancellationToken cancellationToken)
    {
        var membership = await _teams.GetMembershipAsync(teamId, actorUserId, cancellationToken);
        if (membership is null || !membership.CanManageRoles())
        {
            return Result<TeamAuthorization>.Failure(ErrorType.Forbidden, "team.manage_forbidden", "Only team owners can manage this team.");
        }

        var team = membership.Team ?? await _teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null || team.IsArchived)
        {
            return Result<TeamAuthorization>.Failure(ErrorType.NotFound, "team.not_found", "Team was not found.");
        }

        return Result<TeamAuthorization>.Success(new TeamAuthorization(team, membership));
    }

    private static TeamJoinRequestResponse ToJoinRequestResponse(TeamJoinRequest joinRequest)
    {
        var user = joinRequest.User ?? throw new InvalidOperationException("Join request user was not loaded.");
        return new TeamJoinRequestResponse(joinRequest.Id, joinRequest.TeamId, joinRequest.UserId, user.DisplayName, user.Gamertag, joinRequest.Message, (JoinRequestStatus)(int)joinRequest.Status, joinRequest.CreatedAt);
    }

    private static TeamPositionResponse ToPositionResponse(TeamPosition position)
    {
        return new TeamPositionResponse(position.Id, position.Name, position.Description, position.SortOrder);
    }

    private static TeamScheduleSlotResponse ToScheduleSlotResponse(TeamScheduleSlot slot)
    {
        return new TeamScheduleSlotResponse(slot.Id, slot.DayOfWeek, slot.StartTime, slot.EndTime, slot.Label);
    }

    private async Task<Result<PlayerDemandResponse>> UpdatePlayerDemandStatusAsync(Guid actorUserId, Guid teamId, Guid demandId, Action<TeamPlayerDemand> update, CancellationToken cancellationToken)
    {
        var authorization = await EnsureCanManageTeamAsync(actorUserId, teamId, cancellationToken);
        if (!authorization.IsSuccess)
        {
            return Result<PlayerDemandResponse>.Failure(authorization.Error!.Type, authorization.Error.Code, authorization.Error.Message);
        }

        var demand = await _teams.GetPlayerDemandAsync(teamId, demandId, cancellationToken);
        if (demand is null)
        {
            return Result<PlayerDemandResponse>.Failure(ErrorType.NotFound, "team.player_demand_not_found", "Player demand was not found.");
        }

        update(demand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PlayerDemandResponse>.Success(TodayService.ToPlayerDemandResponse(demand));
    }

    private static string CreateInviteCode()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", string.Empty).Replace("/", string.Empty).Replace("=", string.Empty)[..10].ToUpperInvariant();
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record TeamAuthorization(Team Team, TeamMember Membership);
}
