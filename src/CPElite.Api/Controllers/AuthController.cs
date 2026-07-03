using CPElite.Application.Services;
using CPElite.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CPElite.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterPlayerRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _authService.RegisterAsync(request, cancellationToken));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _authService.LoginAsync(request, cancellationToken));
    }
}
