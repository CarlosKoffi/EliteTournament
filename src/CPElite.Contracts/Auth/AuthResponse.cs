namespace CPElite.Contracts.Auth;

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserSummaryResponse User);
