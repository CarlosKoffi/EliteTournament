using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CPElite.Contracts.Common;
using CPElite.Contracts.Teams;
using CPElite.Tests.Integration.Support;

namespace CPElite.Tests.Integration.Teams;

public sealed class TeamManagementTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public TeamManagementTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Owner_can_update_profile_socials_settings_positions_schedule_and_archive()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var team = await _client.CreateTeamAsync($"Elite {Guid.NewGuid():N}");

        var profileResponse = await _client.PatchAsJsonAsync($"/api/teams/{team.Id}/profile", new UpdateTeamProfileRequest(
            "Elite Updated",
            "ELU",
            Platform.CrossPlay,
            "NA",
            "Competitive club",
            123456,
            "https://cdn.example.com/logo.png",
            "https://cdn.example.com/banner.png"), JsonOptions);
        profileResponse.EnsureSuccessStatusCode();
        var updated = (await profileResponse.Content.ReadFromJsonAsync<TeamResponse>(JsonOptions))!;
        Assert.Equal(123456, updated.EaClubId);
        Assert.Equal("https://cdn.example.com/logo.png", updated.LogoUrl);

        var socialsResponse = await _client.PatchAsJsonAsync($"/api/teams/{team.Id}/social-links", new UpdateTeamSocialLinksRequest("https://discord.gg/team", "https://twitch.tv/team", "https://tiktok.com/@team", "https://twitter.com/team"), JsonOptions);
        socialsResponse.EnsureSuccessStatusCode();

        var settingsResponse = await _client.PatchAsJsonAsync($"/api/teams/{team.Id}/settings", new UpdateTeamSettingsRequest(true), JsonOptions);
        settingsResponse.EnsureSuccessStatusCode();

        var positionResponse = await _client.PostAsJsonAsync($"/api/teams/{team.Id}/positions", new CreateTeamPositionRequest("Striker", "Primary scorer", 1), JsonOptions);
        positionResponse.EnsureSuccessStatusCode();

        var scheduleResponse = await _client.PostAsJsonAsync($"/api/teams/{team.Id}/schedule", new CreateTeamScheduleSlotRequest(DayOfWeek.Friday, new TimeOnly(20, 0), new TimeOnly(22, 0), "Training"), JsonOptions);
        scheduleResponse.EnsureSuccessStatusCode();

        var archiveResponse = await _client.DeleteAsync($"/api/teams/{team.Id}");
        archiveResponse.EnsureSuccessStatusCode();
        var archived = (await archiveResponse.Content.ReadFromJsonAsync<TeamResponse>(JsonOptions))!;
        Assert.True(archived.IsArchived);
    }

    [Fact]
    public async Task Expired_invite_code_cannot_be_used()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var team = await _client.CreateTeamAsync($"Elite {Guid.NewGuid():N}");
        var expireResponse = await _client.PostAsync($"/api/teams/{team.Id}/invite/expire", null);
        expireResponse.EnsureSuccessStatusCode();

        var player = await _client.RegisterPlayerAsync($"player-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(player);
        var joinResponse = await _client.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(team.InviteCode), JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, joinResponse.StatusCode);
    }

    [Fact]
    public async Task Owner_can_approve_join_request()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var team = await _client.CreateTeamAsync($"Elite {Guid.NewGuid():N}");

        var player = await _client.RegisterPlayerAsync($"player-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(player);
        var requestResponse = await _client.PostAsJsonAsync($"/api/teams/{team.Id}/join-requests", new CreateJoinRequestRequest("Let me in"), JsonOptions);
        requestResponse.EnsureSuccessStatusCode();
        var request = (await requestResponse.Content.ReadFromJsonAsync<TeamJoinRequestResponse>(JsonOptions))!;

        _client.AuthorizeAs(owner);
        var approveResponse = await _client.PostAsync($"/api/teams/{team.Id}/join-requests/{request.Id}/approve", null);

        approveResponse.EnsureSuccessStatusCode();
        var membership = (await approveResponse.Content.ReadFromJsonAsync<TeamMemberResponse>(JsonOptions))!;
        Assert.Equal(player.User.Id, membership.UserId);
        Assert.Equal(TeamRole.Player, membership.Role);
    }
}
