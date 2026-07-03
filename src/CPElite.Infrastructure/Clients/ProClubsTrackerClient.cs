namespace CPElite.Infrastructure.Clients;

public sealed class ProClubsTrackerClient
{
    private readonly HttpClient _httpClient;

    public ProClubsTrackerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> SearchClubJsonAsync(string platform, string clubName, CancellationToken cancellationToken = default)
    {
        return await GetJsonOrNullAsync($"/api/clubs/search?clubName={Uri.EscapeDataString(clubName)}&platform={Uri.EscapeDataString(platform)}", cancellationToken);
    }

    public async Task<string?> GetClubDetailsJsonAsync(string platform, long clubId, CancellationToken cancellationToken = default)
    {
        return await GetJsonOrNullAsync($"/api/clubs/{clubId}?platform={Uri.EscapeDataString(platform)}", cancellationToken);
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
