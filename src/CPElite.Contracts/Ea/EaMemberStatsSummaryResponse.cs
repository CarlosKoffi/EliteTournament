namespace CPElite.Contracts.Ea;

public sealed record EaMemberStatsSummaryResponse(Guid TeamId, long EaClubId, string Platform, DateTimeOffset SyncedAt, int PlayerCount, IReadOnlyCollection<EaPlayerStatsResponse> Players);
