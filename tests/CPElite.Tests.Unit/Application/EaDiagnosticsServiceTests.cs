using CPElite.Application.Abstractions;
using CPElite.Application.Services;
using CPElite.Contracts.Ea;
using CPElite.Domain.Entities;

namespace CPElite.Tests.Unit.Application;

public sealed class EaDiagnosticsServiceTests
{
    [Fact]
    public async Task Probe_records_success_and_access_denied_steps()
    {
        var client = new FakeEaDiagnosticsClient();
        client.Responses["https://proclubstracker.com/api/clubs/search?clubName=TheSurvivors&platform=common-gen5"] = new EaDiagnosticProbeResult(true, 200, null, """[{"clubId":"123","clubName":"TheSurvivors"}]""");
        client.Responses["/allTimeLeaderboard/search?platform=common-gen5&clubName=TheSurvivors"] = new EaDiagnosticProbeResult(true, 200, null, """[{"clubInfo":{"name":"TheSurvivors","clubId":123}}]""");
        client.Responses["/clubs/search?platform=common-gen5&clubName=TheSurvivors"] = new EaDiagnosticProbeResult(false, 403, "Forbidden", "Access Denied");
        client.Responses["https://proclubstracker.com/api/clubs/123?platform=common-gen5"] = new EaDiagnosticProbeResult(true, 200, null, """{"clubId":"123","memberStats":{"members":[]}}""");
        client.Responses["/clubs/info?platform=common-gen5&clubIds=123"] = new EaDiagnosticProbeResult(true, 200, null, """{"clubs":{"123":{"name":"TheSurvivors"}}}""");
        client.Responses["/clubs/matches?matchType=friendlyMatch&platform=common-gen5&clubIds=123&maxResultCount=10"] = new EaDiagnosticProbeResult(true, 200, null, """{"matches":[]}""");
        client.Responses["/members/stats?platform=common-gen5&clubId=123"] = new EaDiagnosticProbeResult(true, 200, null, """{"members":[]}""");
        client.Responses["/members/career/stats?platform=common-gen5&clubId=123"] = new EaDiagnosticProbeResult(false, 403, "Forbidden", "Access Denied");
        var repository = new FakeEaDiagnosticsRepository();
        var service = new EaDiagnosticsService(client, repository, new FakeClock(), new FakeUnitOfWork());

        var result = await service.ProbeAsync(new EaProbeRequest("common-gen5", "TheSurvivors", 123));

        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Value!.Steps.Count);
        Assert.Contains(result.Value.Steps, step => step.Name == "proclubstracker-search" && step.Success);
        Assert.Contains(result.Value.Steps, step => step.Name == "proclubstracker-club-details" && step.Success);
        Assert.Contains(result.Value.Steps, step => step.Name == "leaderboard-club-search" && step.Success);
        Assert.Contains(result.Value.Steps, step => step.Name == "legacy-club-search" && !step.Success && step.StatusCode == 403);
        Assert.Equal(8, repository.Probes.Count);
    }

    [Fact]
    public async Task Probe_uses_club_id_from_search_when_request_does_not_include_one()
    {
        var client = new FakeEaDiagnosticsClient();
        client.Responses["https://proclubstracker.com/api/clubs/search?clubName=TheSurvivors&platform=common-gen5"] = new EaDiagnosticProbeResult(true, 200, null, """[{"clubId":"2148207","clubName":"TheSurvivors"}]""");
        client.Responses["/allTimeLeaderboard/search?platform=common-gen5&clubName=TheSurvivors"] = new EaDiagnosticProbeResult(false, 403, "Forbidden", "Access Denied");
        client.Responses["/clubs/search?platform=common-gen5&clubName=TheSurvivors"] = new EaDiagnosticProbeResult(false, 403, "Forbidden", "Access Denied");
        client.Responses["https://proclubstracker.com/api/clubs/2148207?platform=common-gen5"] = new EaDiagnosticProbeResult(true, 200, null, """{"clubId":"2148207","memberStats":{"members":[]}}""");
        client.Responses["/clubs/info?platform=common-gen5&clubIds=2148207"] = new EaDiagnosticProbeResult(false, 403, "Forbidden", "Access Denied");
        client.Responses["/clubs/matches?matchType=friendlyMatch&platform=common-gen5&clubIds=2148207&maxResultCount=10"] = new EaDiagnosticProbeResult(false, 403, "Forbidden", "Access Denied");
        client.Responses["/members/stats?platform=common-gen5&clubId=2148207"] = new EaDiagnosticProbeResult(false, 403, "Forbidden", "Access Denied");
        client.Responses["/members/career/stats?platform=common-gen5&clubId=2148207"] = new EaDiagnosticProbeResult(false, 403, "Forbidden", "Access Denied");
        var repository = new FakeEaDiagnosticsRepository();
        var service = new EaDiagnosticsService(client, repository, new FakeClock(), new FakeUnitOfWork());

        var result = await service.ProbeAsync(new EaProbeRequest("common-gen5", "TheSurvivors", null));

        Assert.True(result.IsSuccess);
        Assert.Equal(2148207, result.Value!.ClubId);
        Assert.Contains(result.Value.Steps, step => step.Name == "proclubstracker-club-details" && step.Success);
        Assert.Contains(repository.Probes, probe => probe.ClubId == 2148207 && probe.StepName == "club-info");
    }

    private sealed class FakeEaDiagnosticsClient : IEaDiagnosticsClient
    {
        public Dictionary<string, EaDiagnosticProbeResult> Responses { get; } = [];

        public Task<EaDiagnosticProbeResult> ProbeAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Responses[endpoint]);
        }
    }

    private sealed class FakeEaDiagnosticsRepository : IEaDiagnosticsRepository
    {
        public List<EaDiagnosticProbe> Probes { get; } = [];

        public Task AddAsync(EaDiagnosticProbe probe, CancellationToken cancellationToken = default)
        {
            Probes.Add(probe);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 6, 26, 9, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
