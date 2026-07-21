using CPElite.Application.Services;
using CPElite.Contracts.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/teams")]
public sealed class TeamsController : ApiControllerBase
{
    private readonly TeamService _teamService;

    public TeamsController(TeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.CreateTeamAsync(CurrentUserId, request, cancellationToken));
    }

    [HttpGet("{teamId:guid}")]
    public async Task<IActionResult> Get(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.GetTeamAsync(CurrentUserId, teamId, cancellationToken));
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join(JoinTeamRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.JoinTeamAsync(CurrentUserId, request, cancellationToken));
    }

    [HttpPost("{teamId:guid}/join-existing")]
    public async Task<IActionResult> JoinExisting(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.JoinExistingTeamAsync(CurrentUserId, teamId, cancellationToken));
    }
    [HttpPatch("{teamId:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid teamId, UpdateTeamProfileRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.UpdateProfileAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpPatch("{teamId:guid}/social-links")]
    public async Task<IActionResult> UpdateSocialLinks(Guid teamId, UpdateTeamSocialLinksRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.UpdateSocialLinksAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpPatch("{teamId:guid}/settings")]
    public async Task<IActionResult> UpdateSettings(Guid teamId, UpdateTeamSettingsRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.UpdateSettingsAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpPost("{teamId:guid}/invite/expire")]
    public async Task<IActionResult> ExpireInvite(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.ExpireInviteCodeAsync(CurrentUserId, teamId, cancellationToken));
    }

    [HttpPost("{teamId:guid}/invite/regenerate")]
    public async Task<IActionResult> RegenerateInvite(Guid teamId, RegenerateInviteCodeRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.RegenerateInviteCodeAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Archive(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.ArchiveTeamAsync(CurrentUserId, teamId, cancellationToken));
    }

    [HttpPost("{teamId:guid}/join-requests")]
    public async Task<IActionResult> RequestToJoin(Guid teamId, CreateJoinRequestRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.CreateJoinRequestAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpGet("{teamId:guid}/join-requests")]
    public async Task<IActionResult> GetJoinRequests(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.GetPendingJoinRequestsAsync(CurrentUserId, teamId, cancellationToken));
    }

    [HttpPost("{teamId:guid}/join-requests/{requestId:guid}/approve")]
    public async Task<IActionResult> ApproveJoinRequest(Guid teamId, Guid requestId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.ApproveJoinRequestAsync(CurrentUserId, teamId, requestId, cancellationToken));
    }

    [HttpPost("{teamId:guid}/join-requests/{requestId:guid}/reject")]
    public async Task<IActionResult> RejectJoinRequest(Guid teamId, Guid requestId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.RejectJoinRequestAsync(CurrentUserId, teamId, requestId, cancellationToken));
    }

    [HttpGet("{teamId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.GetMembersAsync(teamId, cancellationToken));
    }

    [HttpPatch("{teamId:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid teamId, Guid userId, ChangeTeamMemberRoleRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.ChangeRoleAsync(CurrentUserId, teamId, userId, request.Role, cancellationToken));
    }

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.RemoveMemberAsync(CurrentUserId, teamId, userId, cancellationToken));
    }

    [HttpPost("{teamId:guid}/positions")]
    public async Task<IActionResult> CreatePosition(Guid teamId, CreateTeamPositionRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.CreatePositionAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpGet("{teamId:guid}/positions")]
    public async Task<IActionResult> GetPositions(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.GetPositionsAsync(teamId, cancellationToken));
    }

    [HttpDelete("{teamId:guid}/positions/{positionId:guid}")]
    public async Task<IActionResult> DeletePosition(Guid teamId, Guid positionId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.DeletePositionAsync(CurrentUserId, teamId, positionId, cancellationToken));
    }

    [HttpPost("{teamId:guid}/schedule")]
    public async Task<IActionResult> CreateScheduleSlot(Guid teamId, CreateTeamScheduleSlotRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.CreateScheduleSlotAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpGet("{teamId:guid}/schedule")]
    public async Task<IActionResult> GetSchedule(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.GetScheduleSlotsAsync(teamId, cancellationToken));
    }

    [HttpDelete("{teamId:guid}/schedule/{slotId:guid}")]
    public async Task<IActionResult> DeleteScheduleSlot(Guid teamId, Guid slotId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.DeleteScheduleSlotAsync(CurrentUserId, teamId, slotId, cancellationToken));
    }

    [HttpPost("{teamId:guid}/player-demands")]
    public async Task<IActionResult> CreatePlayerDemand(Guid teamId, CreatePlayerDemandRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.CreatePlayerDemandAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpPost("{teamId:guid}/player-demands/{demandId:guid}/filled")]
    public async Task<IActionResult> MarkPlayerDemandFilled(Guid teamId, Guid demandId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.MarkPlayerDemandFilledAsync(CurrentUserId, teamId, demandId, cancellationToken));
    }

    [HttpPost("{teamId:guid}/player-demands/{demandId:guid}/cancel")]
    public async Task<IActionResult> CancelPlayerDemand(Guid teamId, Guid demandId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _teamService.CancelPlayerDemandAsync(CurrentUserId, teamId, demandId, cancellationToken));
    }
}
