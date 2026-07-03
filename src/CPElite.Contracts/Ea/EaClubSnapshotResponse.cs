namespace CPElite.Contracts.Ea;

public sealed record EaClubSnapshotResponse(Guid TeamId, long EaClubId, string Platform, string? Name, string? Abbreviation, int? Division, int? MembersCount, DateTimeOffset SyncedAt);
