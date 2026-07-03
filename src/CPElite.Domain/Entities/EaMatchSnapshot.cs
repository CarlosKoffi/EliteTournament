namespace CPElite.Domain.Entities;

public sealed class EaMatchSnapshot
{
    private EaMatchSnapshot() { }

    public EaMatchSnapshot(Guid id, Guid teamId, long eaClubId, string platform, string matchType, string rawJson, DateTimeOffset syncedAt)
    {
        Id = id;
        TeamId = teamId;
        EaClubId = eaClubId;
        Platform = platform;
        MatchType = matchType;
        RawJson = rawJson;
        SyncedAt = syncedAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public long EaClubId { get; private set; }
    public string Platform { get; private set; } = string.Empty;
    public string MatchType { get; private set; } = string.Empty;
    public string RawJson { get; private set; } = "{}";
    public DateTimeOffset SyncedAt { get; private set; }
    public Team? Team { get; private set; }

    public void Refresh(string rawJson, DateTimeOffset syncedAt)
    {
        RawJson = rawJson;
        SyncedAt = syncedAt;
    }
}
