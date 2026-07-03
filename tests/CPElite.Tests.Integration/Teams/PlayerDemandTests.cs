using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CPElite.Contracts.Teams;
using CPElite.Contracts.Today;
using CPElite.Tests.Integration.Support;

namespace CPElite.Tests.Integration.Teams;

public sealed class PlayerDemandTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public PlayerDemandTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Owner_can_post_player_demand_that_appears_on_today_board()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var team = await _client.CreateTeamAsync($"Elite {Guid.NewGuid():N}");
        var neededAt = DateTimeOffset.UtcNow.AddHours(2);

        var demandResponse = await _client.PostAsJsonAsync($"/api/teams/{team.Id}/player-demands", new CreatePlayerDemandRequest("Goalkeeper", neededAt, neededAt.AddHours(1), "Tonight at 22:30 CET"), JsonOptions);
        demandResponse.EnsureSuccessStatusCode();

        var player = await _client.RegisterPlayerAsync($"player-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(player);
        var today = await _client.GetFromJsonAsync<TodayResponse>($"/api/today?date={DateOnly.FromDateTime(neededAt.UtcDateTime):yyyy-MM-dd}", JsonOptions);

        Assert.NotNull(today);
        Assert.Contains(today!.PlayerDemands, demand => demand.TeamId == team.Id && demand.Position == "Goalkeeper");
    }

    [Fact]
    public async Task Player_cannot_post_demand_for_team_they_do_not_own()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var team = await _client.CreateTeamAsync($"Elite {Guid.NewGuid():N}");

        var player = await _client.RegisterPlayerAsync($"player-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(player);
        var neededAt = DateTimeOffset.UtcNow.AddHours(2);
        var response = await _client.PostAsJsonAsync($"/api/teams/{team.Id}/player-demands", new CreatePlayerDemandRequest("Striker", neededAt, null, null), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
