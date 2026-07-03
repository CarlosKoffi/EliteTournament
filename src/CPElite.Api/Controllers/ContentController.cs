using CPElite.Application.Services;
using CPElite.Contracts.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Route("api/content")]
public sealed class ContentController : ApiControllerBase
{
    private readonly ContentService _contentService;

    public ContentController(ContentService contentService)
    {
        _contentService = contentService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? language, CancellationToken cancellationToken)
    {
        return ToActionResult(await _contentService.GetCatalogAsync(language, cancellationToken));
    }

    [Authorize]
    [HttpPut("admin")]
    public async Task<IActionResult> Upsert(UpsertContentItemRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _contentService.UpsertAsync(CurrentUserId, request, cancellationToken));
    }
}
