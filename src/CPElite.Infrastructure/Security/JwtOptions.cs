namespace CPElite.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "CPElite";
    public string Audience { get; set; } = "CPElite";
    public string SigningKey { get; set; } = "CHANGE_THIS_DEVELOPMENT_KEY_TO_A_LONG_SECRET";
    public int ExpiryMinutes { get; set; } = 60;
}
