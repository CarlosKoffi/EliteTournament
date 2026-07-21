using CPElite.Application.Services;
using CPElite.Contracts.Teams;
using CPElite.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/me")]
public sealed class MeController : ApiControllerBase
{
    private readonly AuthService _authService;
    private readonly UploadedImageStorage _storage;

    public MeController(AuthService authService, UploadedImageStorage storage)
    {
        _authService = authService;
        _storage = storage;
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

    [HttpPost("profile-photo")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<TeamAssetUploadResponse>> UploadProfilePhoto(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _storage.SaveAsync(file, "profile-photos", cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
