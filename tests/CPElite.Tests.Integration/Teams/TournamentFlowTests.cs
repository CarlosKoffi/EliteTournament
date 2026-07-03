using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CPElite.Contracts.Common;
using CPElite.Contracts.Tournaments;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using CPElite.Tests.Integration.Support;
using Microsoft.Extensions.DependencyInjection;

namespace CPElite.Tests.Integration.Teams;

public sealed class TournamentFlowTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    private readonly ApiTestFactory _factory;

    public TournamentFlowTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Team_owner_can_register_create_match_and_submit_score()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var home = await _client.CreateTeamAsync($"Home {Guid.NewGuid():N}");
        var away = await _client.CreateTeamAsync($"Away {Guid.NewGuid():N}");

        var tournamentResponse = await _client.PostAsJsonAsync("/api/tournaments/official", new CreateTournamentRequest("Friday Goodies Cup", TournamentType.Goodies, DateTimeOffset.UtcNow.AddDays(1), "Europe/Zurich", 8, 3.99m, "EUR", "Goodies only", 2), JsonOptions);
        tournamentResponse.EnsureSuccessStatusCode();
        var tournament = (await tournamentResponse.Content.ReadFromJsonAsync<TournamentResponse>(JsonOptions))!;
        Assert.True(tournament.IsCashPrize);

        var registrationResponse = await _client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/registrations", new RegisterTeamForTournamentRequest(home.Id, TournamentPaymentMode.ClubManagerPays), JsonOptions);
        registrationResponse.EnsureSuccessStatusCode();

        var matchResponse = await _client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/matches", new CreateTournamentMatchRequest(home.Id, away.Id, 1, DateTimeOffset.UtcNow.AddHours(2)), JsonOptions);
        matchResponse.EnsureSuccessStatusCode();
        var match = (await matchResponse.Content.ReadFromJsonAsync<TournamentMatchResponse>(JsonOptions))!;

        var scoreResponse = await _client.PostAsJsonAsync($"/api/tournaments/matches/{match.Id}/scores", new SubmitMatchScoreRequest(2, 1, null), JsonOptions);
        scoreResponse.EnsureSuccessStatusCode();
        var scored = (await scoreResponse.Content.ReadFromJsonAsync<TournamentMatchResponse>(JsonOptions))!;

        Assert.Equal(2, scored.HomeScore);
        Assert.Equal(home.Id, scored.WinnerTeamId);
    }

    [Fact]
    public async Task Ea_verification_falls_back_to_owner_confirmation_until_matching_is_confident()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var home = await _client.CreateTeamAsync($"Home {Guid.NewGuid():N}");
        var away = await _client.CreateTeamAsync($"Away {Guid.NewGuid():N}");
        var tournamentResponse = await _client.PostAsJsonAsync("/api/tournaments/official", new CreateTournamentRequest("Normal Cup", TournamentType.Normal, DateTimeOffset.UtcNow.AddDays(1), "Europe/Zurich", 8, 0m, "EUR", null, 2), JsonOptions);
        tournamentResponse.EnsureSuccessStatusCode();
        var tournament = (await tournamentResponse.Content.ReadFromJsonAsync<TournamentResponse>(JsonOptions))!;
        Assert.False(tournament.IsCashPrize);
        var matchResponse = await _client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/matches", new CreateTournamentMatchRequest(home.Id, away.Id, 1, DateTimeOffset.UtcNow.AddHours(2)), JsonOptions);
        matchResponse.EnsureSuccessStatusCode();
        var match = (await matchResponse.Content.ReadFromJsonAsync<TournamentMatchResponse>(JsonOptions))!;

        var verifyResponse = await _client.PostAsync($"/api/tournaments/matches/{match.Id}/verify-ea", null);
        verifyResponse.EnsureSuccessStatusCode();
        var verification = (await verifyResponse.Content.ReadFromJsonAsync<EaMatchVerificationResponse>(JsonOptions))!;

        Assert.False(verification.AutoVerified);
        Assert.Equal(TournamentMatchStatus.OwnerConfirmationRequired, verification.Status);
    }

    [Fact]
    public async Task Discord_bot_can_register_team_read_board_and_publish_moments()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var home = await _client.CreateTeamAsync($"Discord Home {Guid.NewGuid():N}");
        var away = await _client.CreateTeamAsync($"Discord Away {Guid.NewGuid():N}");
        var tournamentResponse = await _client.PostAsJsonAsync("/api/tournaments/official", new CreateTournamentRequest("Discord Cup", TournamentType.Goodies, DateTimeOffset.UtcNow.AddDays(1), "Europe/Zurich", 32, 50m, "EUR", "Goodies", 24, DateTimeOffset.UtcNow.AddHours(3), 545m), JsonOptions);
        tournamentResponse.EnsureSuccessStatusCode();
        var tournament = (await tournamentResponse.Content.ReadFromJsonAsync<TournamentResponse>(JsonOptions))!;

        _client.DefaultRequestHeaders.Remove("X-CPElite-Discord-Key");
        _client.DefaultRequestHeaders.Add("X-CPElite-Discord-Key", "test-discord-key");
        var discordRegistration = await _client.PostAsJsonAsync($"/api/discord/tournaments/{tournament.Id}/registrations/by-team-name", new DiscordTournamentRegistrationRequest(home.Name, "guild-1", "channel-1", "message-1", "discord-owner-1"), JsonOptions);
        discordRegistration.EnsureSuccessStatusCode();
        var registration = (await discordRegistration.Content.ReadFromJsonAsync<DiscordTournamentRegistrationResponse>(JsonOptions))!;
        Assert.Equal(DiscordTournamentRegistrationOutcome.Registered, registration.Outcome);
        Assert.True(registration.RequiresAppAction);

        var boardResponse = await _client.GetAsync($"/api/discord/tournaments/{tournament.Id}/registrations");
        boardResponse.EnsureSuccessStatusCode();
        var board = (await boardResponse.Content.ReadFromJsonAsync<TournamentRegistrationSummaryResponse>(JsonOptions))!;
        Assert.Single(board.Teams);

        var matchResponse = await _client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/matches", new CreateTournamentMatchRequest(home.Id, away.Id, 1, DateTimeOffset.UtcNow.AddHours(4)), JsonOptions);
        matchResponse.EnsureSuccessStatusCode();
        var match = (await matchResponse.Content.ReadFromJsonAsync<TournamentMatchResponse>(JsonOptions))!;

        var momentResponse = await _client.PostAsJsonAsync($"/api/discord/tournaments/matches/{match.Id}/moments", new CreateTournamentMomentRequest(TournamentMomentType.NextMatchReady, home.Id, null, "discord-owner-1", "Next game", "Your next opponent is ready.", null), JsonOptions);
        momentResponse.EnsureSuccessStatusCode();
        var moment = (await momentResponse.Content.ReadFromJsonAsync<TournamentMomentResponse>(JsonOptions))!;

        var pendingResponse = await _client.GetAsync("/api/discord/tournaments/moments/pending?take=10");
        pendingResponse.EnsureSuccessStatusCode();
        var pending = (await pendingResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<TournamentMomentResponse>>(JsonOptions))!;
        Assert.Contains(pending, item => item.Id == moment.Id);

        var publishedResponse = await _client.PostAsync($"/api/discord/tournaments/moments/{moment.Id}/mark-published", null);
        publishedResponse.EnsureSuccessStatusCode();
        var published = (await publishedResponse.Content.ReadFromJsonAsync<TournamentMomentResponse>(JsonOptions))!;
        Assert.True(published.IsPublishedToDiscord);
    }

    [Fact]
    public async Task Admin_can_launch_world_cup_draw_with_groups_and_knockout_plan()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);

        var teams = new List<CPElite.Contracts.Teams.TeamResponse>();
        for (var i = 1; i <= 8; i++)
        {
            teams.Add(await _client.CreateTeamAsync($"Draw Team {i} {Guid.NewGuid():N}"));
        }

        var tournamentResponse = await _client.PostAsJsonAsync("/api/tournaments/official", new CreateTournamentRequest("Group Stage Format", TournamentType.Goodies, DateTimeOffset.UtcNow.AddDays(1), "Europe/Zurich", 8, 50m, "EUR", "Goodies", 8, DateTimeOffset.UtcNow.AddHours(3), 700m, IsCashPrize: false), JsonOptions);
        tournamentResponse.EnsureSuccessStatusCode();
        var tournament = (await tournamentResponse.Content.ReadFromJsonAsync<TournamentResponse>(JsonOptions))!;

        foreach (var team in teams)
        {
            var registrationResponse = await _client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/registrations", new RegisterTeamForTournamentRequest(team.Id, TournamentPaymentMode.ClubManagerPays), JsonOptions);
            registrationResponse.EnsureSuccessStatusCode();
        }

        var drawResponse = await _client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/launch-world-cup", new LaunchWorldCupTournamentRequest(GroupSize: 4, QualifiersPerGroup: 2, MinutesBetweenGroupMatches: 20, DrawSeed: 26), JsonOptions);
        drawResponse.EnsureSuccessStatusCode();
        var draw = (await drawResponse.Content.ReadFromJsonAsync<TournamentDrawResponse>(JsonOptions))!;

        Assert.Equal(8, draw.TeamCount);
        Assert.Equal(2, draw.GroupCount);
        Assert.Equal(2, draw.Groups.Count);
        Assert.All(draw.Groups, group => Assert.Equal(4, group.Teams.Count));
        Assert.Equal(12, draw.GroupMatches.Count);
        Assert.All(draw.GroupMatches, match => Assert.Equal(CPElite.Contracts.Common.TournamentStage.Group, match.Stage));
        Assert.Contains(draw.KnockoutPlan, round => round.Stage == CPElite.Contracts.Common.TournamentStage.SemiFinal);
        Assert.Contains(draw.KnockoutPlan, round => round.Stage == CPElite.Contracts.Common.TournamentStage.Final);
    }

    [Fact]
    public async Task Ea_verification_prefers_finished_rematch_over_short_restart_inside_monitoring_window()
    {
        var owner = await _client.RegisterPlayerAsync($"owner-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(owner);
        var home = await _client.CreateTeamAsync($"Ea Home {Guid.NewGuid():N}");
        var away = await _client.CreateTeamAsync($"Ea Away {Guid.NewGuid():N}");

        home = await UpdateEaClubAsync(home, 111111);
        away = await UpdateEaClubAsync(away, 222222);

        var scheduledAt = new DateTimeOffset(2026, 6, 26, 23, 15, 0, TimeSpan.Zero);
        var tournamentResponse = await _client.PostAsJsonAsync("/api/tournaments/official", new CreateTournamentRequest("EA Weird Cup", TournamentType.Goodies, scheduledAt, "Europe/Zurich", 32, 50m, "EUR", "Goodies", 24, scheduledAt.AddHours(-5), 545m, 15, 60), JsonOptions);
        tournamentResponse.EnsureSuccessStatusCode();
        var tournament = (await tournamentResponse.Content.ReadFromJsonAsync<TournamentResponse>(JsonOptions))!;

        var matchResponse = await _client.PostAsJsonAsync($"/api/tournaments/{tournament.Id}/matches", new CreateTournamentMatchRequest(home.Id, away.Id, 1, scheduledAt), JsonOptions);
        matchResponse.EnsureSuccessStatusCode();
        var match = (await matchResponse.Content.ReadFromJsonAsync<TournamentMatchResponse>(JsonOptions))!;

        var weirdEaJson = """
        {
          "matches": [
            {
              "matchId": "restart-3-min",
              "timestamp": "2026-06-26T23:18:00Z",
              "status": "restarted",
              "durationMinutes": 3,
              "clubs": {
                "111111": { "clubId": 111111, "score": 0 },
                "222222": { "clubId": 222222, "score": 0 }
              }
            },
            {
              "matchId": "finished-rematch",
              "timestamp": "2026-06-26T23:21:00Z",
              "status": "finished",
              "durationMinutes": 15,
              "clubs": {
                "111111": { "clubId": 111111, "score": 2 },
                "222222": { "clubId": 222222, "score": 1 }
              },
              "players": [
                { "eaSportsId": "home-st", "heightCm": 188, "goals": 2 },
                { "eaSportsId": "away-cam", "heightCm": 181, "goals": 1 }
              ]
            }
          ]
        }
        """;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CPEliteDbContext>();
            db.EaMatchSnapshots.Add(new EaMatchSnapshot(Guid.NewGuid(), home.Id, 111111, "common-gen5", "friendlyMatch", weirdEaJson, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var verificationResponse = await _client.PostAsync($"/api/tournaments/matches/{match.Id}/verify-ea", null);
        verificationResponse.EnsureSuccessStatusCode();
        var verification = (await verificationResponse.Content.ReadFromJsonAsync<EaMatchVerificationResponse>(JsonOptions))!;

        Assert.True(verification.RequiresOwnerValidation);
        Assert.True(verification.Confidence >= 80);
        Assert.Equal(2, verification.HomeScore);
        Assert.Equal(1, verification.AwayScore);
        Assert.Contains("finished", verification.Evidence, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<CPElite.Contracts.Teams.TeamResponse> UpdateEaClubAsync(CPElite.Contracts.Teams.TeamResponse team, long eaClubId)
    {
        var response = await _client.PatchAsJsonAsync($"/api/teams/{team.Id}/profile", new CPElite.Contracts.Teams.UpdateTeamProfileRequest(
            team.Name,
            team.ShortName,
            team.Platform,
            team.Region,
            team.Description,
            eaClubId,
            team.LogoUrl,
            team.BannerUrl), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CPElite.Contracts.Teams.TeamResponse>(JsonOptions))!;
    }
}
