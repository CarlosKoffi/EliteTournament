using CPElite.Contracts.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/team-assets")]
public sealed class TeamAssetsController : ControllerBase
{
    private readonly UploadedImageStorage _storage;

    public TeamAssetsController(UploadedImageStorage storage)
    {
        _storage = storage;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<TeamAssetUploadResponse>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _storage.SaveAsync(file, "team-assets", cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
