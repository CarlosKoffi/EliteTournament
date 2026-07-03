using CPElite.Application.Abstractions;

namespace CPElite.Infrastructure.Clients;

public sealed class DirectEaProClubsClient
{
    private readonly HttpClient _httpClient;

    public DirectEaProClubsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<string?> GetClubInfoJsonAsync(string platform, long clubId, CancellationToken cancellationToken = default)
    {
        return GetJsonOrNullAsync($"/clubs/info?platform={Uri.EscapeDataString(platform)}&clubIds={clubId}", cancellationToken);
    }

    public Task<string?> GetMemberStatsJsonAsync(string platform, long clubId, CancellationToken cancellationToken = default)
    {
        return GetJsonOrNullAsync($"/members/stats?platform={Uri.EscapeDataString(platform)}&clubId={clubId}", cancellationToken);
    }

    public Task<string?> GetClubMatchesJsonAsync(string platform, long clubId, string matchType, int maxResults, CancellationToken cancellationToken = default)
    {
        return GetJsonOrNullAsync($"/clubs/matches?matchType={Uri.EscapeDataString(matchType)}&platform={Uri.EscapeDataString(platform)}&clubIds={clubId}&maxResultCount={maxResults}", cancellationToken);
    }

    private async Task<string?> GetJsonOrNullAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path.TrimStart('/'), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
