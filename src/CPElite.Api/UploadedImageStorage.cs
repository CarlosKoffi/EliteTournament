using CPElite.Contracts.Teams;

namespace CPElite.Api;

public sealed class UploadedImageStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly IWebHostEnvironment _environment;

    public UploadedImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<TeamAssetUploadResponse> SaveAsync(IFormFile file, string folderName, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Image file is required.");
        }

        if (file.Length > 5_000_000)
        {
            throw new InvalidOperationException("Image must be 5 MB or smaller.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Only jpeg, png, webp, and gif images are allowed.");
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
        var safeFolderName = folderName.Replace("\\", "/", StringComparison.Ordinal).Trim('/');
        var uploadDirectory = Path.Combine(webRootPath, "uploads", safeFolderName);
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadDirectory, fileName);
        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return new TeamAssetUploadResponse($"/uploads/{safeFolderName}/{fileName}", fileName, file.Length, file.ContentType);
    }
}
