using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CPElite.Contracts.Auth;
using CPElite.Contracts.Common;
using CPElite.Contracts.Teams;

namespace CPElite.Tests.Integration.Support;

internal static class ApiClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<AuthResponse> RegisterPlayerAsync(this HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterPlayerRequest(email, "Password123", email.Split('@')[0], email.Split('@')[0], $"ea-{email.Split('@')[0]}", Platform.CrossPlay, "en", "UTC"), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
    }

    public static async Task<TeamResponse> CreateTeamAsync(this HttpClient client, string teamName)
    {
        var response = await client.PostAsJsonAsync("/api/teams", new CreateTeamRequest(teamName, "CPE", Platform.CrossPlay, "EU", null, null, null, null), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TeamResponse>(JsonOptions))!;
    }

    public static void AuthorizeAs(this HttpClient client, AuthResponse auth)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }
}
