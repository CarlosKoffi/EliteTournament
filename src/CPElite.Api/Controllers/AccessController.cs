using CPElite.Application.Services;
using CPElite.Contracts.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/access")]
public sealed class AccessController : ApiControllerBase
{
    private readonly AccessService _accessService;

    public AccessController(AccessService accessService)
    {
        _accessService = accessService;
    }

    [HttpPost("individual")]
    public async Task<IActionResult> PurchaseIndividual(PurchaseIndividualAccessRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _accessService.PurchaseIndividualAccessAsync(CurrentUserId, request, cancellationToken));
    }

    [HttpPost("teams/{teamId:guid}/slots")]
    public async Task<IActionResult> PurchaseTeamSlots(Guid teamId, PurchaseTeamSlotsRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _accessService.PurchaseTeamSlotsAsync(CurrentUserId, teamId, request, cancellationToken));
    }

    [HttpGet("teams/{teamId:guid}/slots")]
    public async Task<IActionResult> GetTeamSlots(Guid teamId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _accessService.GetTeamAccessSummaryAsync(CurrentUserId, teamId, cancellationToken));
    }

    [HttpGet("teams/{teamId:guid}/players/{userId:guid}/signing-check")]
    public async Task<IActionResult> CheckPlayerSigning(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _accessService.CheckPlayerSigningAccessAsync(CurrentUserId, teamId, userId, cancellationToken));
    }

    [HttpPost("teams/{teamId:guid}/slots/assign")]
    public async Task<IActionResult> AssignTeamSlot(Guid teamId, AssignTeamSlotRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _accessService.AssignTeamSlotAsync(CurrentUserId, teamId, request, cancellationToken));
    }
}
