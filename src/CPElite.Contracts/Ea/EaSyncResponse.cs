namespace CPElite.Contracts.Ea;

public sealed record EaSyncResponse(Guid TeamId, long EaClubId, string Platform, DateTimeOffset SyncedAt, bool ClubInfoSynced, bool MemberStatsSynced, int MatchTypesSynced);
