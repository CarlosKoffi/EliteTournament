using CPElite.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Authorize]
[Route("api/today")]
public sealed class TodayController : ApiControllerBase
{
    private readonly TodayService _todayService;

    public TodayController(TodayService todayService)
    {
        _todayService = todayService;
    }

    [HttpGet]
    public async Task<IActionResult> GetToday([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        return ToActionResult(await _todayService.GetTodayAsync(date, cancellationToken));
    }
}
