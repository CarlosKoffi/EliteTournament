using CPElite.Application.Abstractions;

namespace CPElite.Infrastructure.Clients;

public sealed class EaProClubsClient : IEaProClubsClient
{
    private readonly DirectEaProClubsClient _directEa;
    private readonly ProClubsTrackerClient _proClubsTracker;

    public EaProClubsClient(DirectEaProClubsClient directEa, ProClubsTrackerClient proClubsTracker)
    {
        _directEa = directEa;
        _proClubsTracker = proClubsTracker;
    }

    public async Task<string?> GetClubInfoJsonAsync(string platform, long clubId, CancellationToken cancellationToken = default)
    {
        return await _directEa.GetClubInfoJsonAsync(platform, clubId, cancellationToken)
            ?? await _proClubsTracker.GetClubDetailsJsonAsync(platform, clubId, cancellationToken);
    }

    public async Task<string?> GetMemberStatsJsonAsync(string platform, long clubId, CancellationToken cancellationToken = default)
    {
        return await _directEa.GetMemberStatsJsonAsync(platform, clubId, cancellationToken)
            ?? await _proClubsTracker.GetClubDetailsJsonAsync(platform, clubId, cancellationToken);
    }

    public async Task<string?> GetClubMatchesJsonAsync(string platform, long clubId, string matchType, int maxResults, CancellationToken cancellationToken = default)
    {
        return await _directEa.GetClubMatchesJsonAsync(platform, clubId, matchType, maxResults, cancellationToken)
            ?? await _proClubsTracker.GetClubDetailsJsonAsync(platform, clubId, cancellationToken);
    }
}
