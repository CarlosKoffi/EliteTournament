namespace CPElite.Domain.Entities;

public sealed class EaFriendlyMatch
{
    private EaFriendlyMatch() { }

    public EaFriendlyMatch(Guid id, Guid teamId, long eaClubId, string platform, string eaMatchId, DateTimeOffset playedAt, string matchType, long homeEaClubId, string? homeClubName, int homeScore, long awayEaClubId, string? awayClubName, int awayScore, string rawJson, DateTimeOffset syncedAt)
    {
        Id = id;
        TeamId = teamId;
        EaClubId = eaClubId;
        Platform = platform;
        EaMatchId = eaMatchId;
        PlayedAt = playedAt;
        MatchType = matchType;
        HomeEaClubId = homeEaClubId;
        HomeClubName = homeClubName;
        HomeScore = homeScore;
        AwayEaClubId = awayEaClubId;
        AwayClubName = awayClubName;
        AwayScore = awayScore;
        RawJson = rawJson;
        SyncedAt = syncedAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public long EaClubId { get; private set; }
    public string Platform { get; private set; } = string.Empty;
    public string EaMatchId { get; private set; } = string.Empty;
    public DateTimeOffset PlayedAt { get; private set; }
    public string MatchType { get; private set; } = "friendlyMatch";
    public long HomeEaClubId { get; private set; }
    public string? HomeClubName { get; private set; }
    public int HomeScore { get; private set; }
    public long AwayEaClubId { get; private set; }
    public string? AwayClubName { get; private set; }
    public int AwayScore { get; private set; }
    public string RawJson { get; private set; } = "{}";
    public DateTimeOffset SyncedAt { get; private set; }
    public Guid? TournamentMatchId { get; private set; }
    public Team? Team { get; private set; }
    public IReadOnlyCollection<EaMatchPlayerStat> PlayerStats => _playerStats;
    public IReadOnlyCollection<EaMatchClubStat> ClubStats => _clubStats;

    private readonly List<EaMatchPlayerStat> _playerStats = [];
    private readonly List<EaMatchClubStat> _clubStats = [];

    public void LinkToTournamentMatch(Guid tournamentMatchId)
    {
        TournamentMatchId = tournamentMatchId;
    }
}
