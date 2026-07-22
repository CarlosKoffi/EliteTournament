using CPElite.Contracts.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/tournament-assets")]
public sealed class TournamentAssetsController : ControllerBase
{
    private readonly UploadedImageStorage _storage;

    public TournamentAssetsController(UploadedImageStorage storage)
    {
        _storage = storage;
    }

    [HttpPost("banner")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<TeamAssetUploadResponse>> UploadBanner(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _storage.SaveAsync(file, "tournament-banners", cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
