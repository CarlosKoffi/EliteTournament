namespace CPElite.Contracts.Teams;

public sealed record UpdateTeamSocialLinksRequest(
    string? DiscordUrl,
    string? TwitchUrl,
    string? TikTokUrl,
    string? TwitterUrl);
