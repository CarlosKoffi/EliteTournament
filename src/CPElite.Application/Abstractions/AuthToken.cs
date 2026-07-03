namespace CPElite.Application.Abstractions;

public sealed record AuthToken(string AccessToken, DateTimeOffset ExpiresAt);
