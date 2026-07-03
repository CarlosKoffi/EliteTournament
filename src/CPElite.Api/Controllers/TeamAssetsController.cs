using CPElite.Contracts.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/team-assets")]
public sealed class TeamAssetsController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly IWebHostEnvironment _environment;

    public TeamAssetsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<TeamAssetUploadResponse>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("Image file is required.");
        }

        if (file.Length > 5_000_000)
        {
            return BadRequest("Image must be 5 MB or smaller.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest("Only jpeg, png, webp, and gif images are allowed.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
        {
            extension = file.ContentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };
        }

        var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var uploadDirectory = Path.Combine(webRootPath, "uploads", "team-assets");
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadDirectory, fileName);
        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"{Request.Scheme}://{Request.Host}/uploads/team-assets/{fileName}";
        return Ok(new TeamAssetUploadResponse(url, fileName, file.Length, file.ContentType));
    }
}
