namespace CPElite.Domain.Entities;

public sealed class EaClubSnapshot
{
    private EaClubSnapshot() { }

    public EaClubSnapshot(Guid id, Guid teamId, long eaClubId, string platform, string? name, string? abbreviation, int? division, int? membersCount, string rawJson, DateTimeOffset syncedAt)
    {
        Id = id;
        TeamId = teamId;
        EaClubId = eaClubId;
        Platform = platform;
        Name = name;
        Abbreviation = abbreviation;
        Division = division;
        MembersCount = membersCount;
        RawJson = rawJson;
        SyncedAt = syncedAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public long EaClubId { get; private set; }
    public string Platform { get; private set; } = string.Empty;
    public string? Name { get; private set; }
    public string? Abbreviation { get; private set; }
    public int? Division { get; private set; }
    public int? MembersCount { get; private set; }
    public string RawJson { get; private set; } = "{}";
    public DateTimeOffset SyncedAt { get; private set; }
    public Team? Team { get; private set; }

    public void Refresh(string? name, string? abbreviation, int? division, int? membersCount, string rawJson, DateTimeOffset syncedAt)
    {
        Name = name;
        Abbreviation = abbreviation;
        Division = division;
        MembersCount = membersCount;
        RawJson = rawJson;
        SyncedAt = syncedAt;
    }
}
