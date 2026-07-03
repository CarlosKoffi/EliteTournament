namespace CPElite.Application.Abstractions;

public interface IEaProClubsClient
{
    Task<string?> GetClubInfoJsonAsync(string platform, long clubId, CancellationToken cancellationToken = default);
    Task<string?> GetMemberStatsJsonAsync(string platform, long clubId, CancellationToken cancellationToken = default);
    Task<string?> GetClubMatchesJsonAsync(string platform, long clubId, string matchType, int maxResults, CancellationToken cancellationToken = default);
}
