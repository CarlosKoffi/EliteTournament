using CPElite.Contracts.Teams;

namespace CPElite.Contracts.Today;

public sealed record TodayResponse(IReadOnlyCollection<PlayerDemandResponse> PlayerDemands);
