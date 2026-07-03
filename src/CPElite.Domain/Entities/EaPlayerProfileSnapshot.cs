namespace CPElite.Domain.Entities;

public sealed class EaPlayerProfileSnapshot
{
    private EaPlayerProfileSnapshot() { }

    public EaPlayerProfileSnapshot(Guid id, Guid teamId, long eaClubId, string platform, string eaPlayerId, string playerName, string? proName, string? position, int? matches, int? winRate, int? goals, int? assists, double? averageRating, int? height, int? weight, int? overall, int? shots, int? shotSuccessRate, int? passesMade, int? passAttempts, double? passSuccessRate, int? tacklesMade, int? tackleAttempts, double? tackleSuccessRate, int? saves, int? cleanSheets, int? cleanSheetsGk, int? playerOfTheMatch, int? redCards, int? prevGoals, string rawJson, DateTimeOffset syncedAt)
    {
        Id = id;
        TeamId = teamId;
        EaClubId = eaClubId;
        Platform = platform;
        EaPlayerId = eaPlayerId;
        PlayerName = playerName;
        ProName = proName;
        Position = position;
        Matches = matches;
        WinRate = winRate;
        Goals = goals;
        Assists = assists;
        AverageRating = averageRating;
        Height = height;
        Weight = weight;
        Overall = overall;
        Shots = shots;
        ShotSuccessRate = shotSuccessRate;
        PassesMade = passesMade;
        PassAttempts = passAttempts;
        StoredPassSuccessRate = passSuccessRate;
        TacklesMade = tacklesMade;
        TackleAttempts = tackleAttempts;
        StoredTackleSuccessRate = tackleSuccessRate;
        Saves = saves;
        CleanSheets = cleanSheets;
        CleanSheetsGk = cleanSheetsGk;
        PlayerOfTheMatch = playerOfTheMatch;
        RedCards = redCards;
        PrevGoals = prevGoals;
        RawJson = rawJson;
        SyncedAt = syncedAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public long EaClubId { get; private set; }
    public string Platform { get; private set; } = string.Empty;
    public string EaPlayerId { get; private set; } = string.Empty;
    public string PlayerName { get; private set; } = string.Empty;
    public string? ProName { get; private set; }
    public string? Position { get; private set; }
    public int? Matches { get; private set; }
    public int? WinRate { get; private set; }
    public int? Goals { get; private set; }
    public int? Assists { get; private set; }
    public double? AverageRating { get; private set; }
    public int? Height { get; private set; }
    public int? Weight { get; private set; }
    public int? Overall { get; private set; }
    public int? Shots { get; private set; }
    public int? ShotSuccessRate { get; private set; }
    public int? PassesMade { get; private set; }
    public int? PassAttempts { get; private set; }
    public double? StoredPassSuccessRate { get; private set; }
    public int? TacklesMade { get; private set; }
    public int? TackleAttempts { get; private set; }
    public double? StoredTackleSuccessRate { get; private set; }
    public int? Saves { get; private set; }
    public int? CleanSheets { get; private set; }
    public int? CleanSheetsGk { get; private set; }
    public int? PlayerOfTheMatch { get; private set; }
    public int? RedCards { get; private set; }
    public int? PrevGoals { get; private set; }
    public string RawJson { get; private set; } = "{}";
    public DateTimeOffset SyncedAt { get; private set; }
    public Team? Team { get; private set; }

    public double? PassSuccessRate => StoredPassSuccessRate ?? (PassAttempts > 0 && PassesMade is not null ? Math.Round((double)PassesMade.Value / PassAttempts.Value * 100, 2) : null);
    public double? TackleSuccessRate => StoredTackleSuccessRate ?? (TackleAttempts > 0 && TacklesMade is not null ? Math.Round((double)TacklesMade.Value / TackleAttempts.Value * 100, 2) : null);
}
