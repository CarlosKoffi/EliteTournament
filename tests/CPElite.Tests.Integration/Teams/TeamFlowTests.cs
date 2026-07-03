using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CPElite.Contracts.Common;
using CPElite.Contracts.Teams;
using CPElite.Tests.Integration.Support;

namespace CPElite.Tests.Integration.Teams;

public sealed class TeamFlowTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public TeamFlowTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Owner_can_create_team_and_player_can_join_by_invite()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var team = await _client.CreateTeamAsync($"Elite {Guid.NewGuid():N}");

        var player = await _client.RegisterPlayerAsync($"player-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(player);
        var joinResponse = await _client.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(team.InviteCode), JsonOptions);

        joinResponse.EnsureSuccessStatusCode();
        var joinedTeam = (await joinResponse.Content.ReadFromJsonAsync<TeamResponse>(JsonOptions))!;
        Assert.Equal(team.Id, joinedTeam.Id);
        Assert.Equal(TeamRole.Player, joinedTeam.CurrentUserMembership.Role);
    }

    [Fact]
    public async Task Player_cannot_change_member_roles()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var team = await _client.CreateTeamAsync($"Elite {Guid.NewGuid():N}");

        var player = await _client.RegisterPlayerAsync($"player-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(player);
        await _client.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(team.InviteCode), JsonOptions);

        var response = await _client.PatchAsJsonAsync($"/api/teams/{team.Id}/members/{owner.User.Id}/role", new ChangeTeamMemberRoleRequest(TeamRole.Player), JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_can_promote_player()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var team = await _client.CreateTeamAsync($"Elite {Guid.NewGuid():N}");

        var player = await _client.RegisterPlayerAsync($"player-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(player);
        await _client.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(team.InviteCode), JsonOptions);

        _client.AuthorizeAs(owner);
        var response = await _client.PatchAsJsonAsync($"/api/teams/{team.Id}/members/{player.User.Id}/role", new ChangeTeamMemberRoleRequest(TeamRole.Captain), JsonOptions);

        response.EnsureSuccessStatusCode();
        var promoted = (await response.Content.ReadFromJsonAsync<TeamMemberResponse>(JsonOptions))!;
        Assert.Equal(TeamRole.Captain, promoted.Role);
    }
}
