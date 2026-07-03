using CPElite.Application.Abstractions;
using CPElite.Contracts.Teams;
using CPElite.Contracts.Today;

namespace CPElite.Application.Services;

public sealed class TodayService
{
    private readonly ITeamRepository _teams;
    private readonly IClock _clock;

    public TodayService(ITeamRepository teams, IClock clock)
    {
        _teams = teams;
        _clock = clock;
    }

    public async Task<Result<TodayResponse>> GetTodayAsync(DateOnly? date, CancellationToken cancellationToken = default)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var start = new DateTimeOffset(selectedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);
        var demands = await _teams.GetActivePlayerDemandsAsync(start, end, _clock.UtcNow, cancellationToken);

        return Result<TodayResponse>.Success(new TodayResponse(demands.Select(ToPlayerDemandResponse).ToArray()));
    }

    internal static PlayerDemandResponse ToPlayerDemandResponse(CPElite.Domain.Entities.TeamPlayerDemand demand)
    {
        var team = demand.Team ?? throw new InvalidOperationException("Player demand team was not loaded.");
        return new PlayerDemandResponse(
            demand.Id,
            demand.TeamId,
            team.Name,
            team.ShortName,
            team.LogoUrl,
            demand.Position,
            demand.NeededAt,
            demand.ExpiresAt,
            demand.Note,
            (Contracts.Common.PlayerDemandStatus)(int)demand.Status,
            demand.CreatedAt);
    }
}
