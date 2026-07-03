using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CPElite.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AuthToken Create(User user)
    {
        var expiresAt = _clock.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName)
        };

        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expiresAt.UtcDateTime, signingCredentials: credentials);
        return new AuthToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
