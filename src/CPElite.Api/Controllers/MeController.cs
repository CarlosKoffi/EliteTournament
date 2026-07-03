using CPElite.Application.Services;
using CPElite.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/me")]
public sealed class MeController : ApiControllerBase
{
    private readonly AuthService _authService;

    public MeController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        return ToActionResult(await _authService.GetMeAsync(CurrentUserId, cancellationToken));
    }

    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile(UpdatePlayerProfileRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _authService.UpdatePlayerProfileAsync(CurrentUserId, request, cancellationToken));
    }
}
