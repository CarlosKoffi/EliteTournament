namespace CPElite.Domain.Entities;

public sealed class EaPlayerProfileSnapshot
{
    private EaPlayerProfileSnapshot() { }

    public EaPlayerProfileSnapshot(Guid id, Guid teamId, long eaClubId, string platform, string eaPlayerId, string playerName, string? proName, string? position, int? matches, int? winRate, int? goals, int? assists, double? averageRating, int? height, int? weight, int? overall, int? shots, int? shotSuccessRate, int? passesMade, int? passAttempts, double? passSuccessRate, int? tacklesMade, int? tackleAttempts, double? tackleSuccessRate, int? saves, int? cleanSheets, int? cleanSheetsGk, int? playerOfTheMatch, int? redCards, int? prevGoals, double? goalsPerMatch, double? assistsPerMatch, int? goalContributions, double? goalContributionsPerMatch, double? passesMadePerMatch, double? tacklesMadePerMatch, double? playerOfTheMatchRate, double? cleanSheetsDefRate, double? cleanSheetsGkRate, int? allClubsMatches, int? allClubsGoals, int? allClubsAssists, double? allClubsAverageRating, int? allClubsPlayerOfTheMatch, double? allClubsPlayerOfTheMatchRate, int? allClubsGoalContributions, double? allClubsGoalContributionsPerMatch, string rawJson, DateTimeOffset syncedAt)
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
        StoredGoalsPerMatch = goalsPerMatch;
        StoredAssistsPerMatch = assistsPerMatch;
        StoredGoalContributions = goalContributions;
        StoredGoalContributionsPerMatch = goalContributionsPerMatch;
        StoredPassesMadePerMatch = passesMadePerMatch;
        StoredTacklesMadePerMatch = tacklesMadePerMatch;
        StoredPlayerOfTheMatchRate = playerOfTheMatchRate;
        StoredCleanSheetsDefRate = cleanSheetsDefRate;
        StoredCleanSheetsGkRate = cleanSheetsGkRate;
        AllClubsMatches = allClubsMatches;
        AllClubsGoals = allClubsGoals;
        AllClubsAssists = allClubsAssists;
        AllClubsAverageRating = allClubsAverageRating;
        AllClubsPlayerOfTheMatch = allClubsPlayerOfTheMatch;
        StoredAllClubsPlayerOfTheMatchRate = allClubsPlayerOfTheMatchRate;
        StoredAllClubsGoalContributions = allClubsGoalContributions;
        StoredAllClubsGoalContributionsPerMatch = allClubsGoalContributionsPerMatch;
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
    public double? StoredGoalsPerMatch { get; private set; }
    public double? StoredAssistsPerMatch { get; private set; }
    public int? StoredGoalContributions { get; private set; }
    public double? StoredGoalContributionsPerMatch { get; private set; }
    public double? StoredPassesMadePerMatch { get; private set; }
    public double? StoredTacklesMadePerMatch { get; private set; }
    public double? StoredPlayerOfTheMatchRate { get; private set; }
    public double? StoredCleanSheetsDefRate { get; private set; }
    public double? StoredCleanSheetsGkRate { get; private set; }
    public int? AllClubsMatches { get; private set; }
    public int? AllClubsGoals { get; private set; }
    public int? AllClubsAssists { get; private set; }
    public double? AllClubsAverageRating { get; private set; }
    public int? AllClubsPlayerOfTheMatch { get; private set; }
    public double? StoredAllClubsPlayerOfTheMatchRate { get; private set; }
    public int? StoredAllClubsGoalContributions { get; private set; }
    public double? StoredAllClubsGoalContributionsPerMatch { get; private set; }
    public string RawJson { get; private set; } = "{}";
    public DateTimeOffset SyncedAt { get; private set; }
    public Team? Team { get; private set; }

    public double? PassSuccessRate => StoredPassSuccessRate ?? (PassAttempts > 0 && PassesMade is not null ? Math.Round((double)PassesMade.Value / PassAttempts.Value * 100, 2) : null);
    public double? TackleSuccessRate => StoredTackleSuccessRate ?? (TackleAttempts > 0 && TacklesMade is not null ? Math.Round((double)TacklesMade.Value / TackleAttempts.Value * 100, 2) : null);
    public double? GoalsPerMatch => StoredGoalsPerMatch ?? PerMatch(Goals, Matches);
    public double? AssistsPerMatch => StoredAssistsPerMatch ?? PerMatch(Assists, Matches);
    public int? GoalContributions => StoredGoalContributions ?? (Goals is null && Assists is null ? null : (Goals ?? 0) + (Assists ?? 0));
    public double? GoalContributionsPerMatch => StoredGoalContributionsPerMatch ?? PerMatch(GoalContributions, Matches);
    public double? PassesMadePerMatch => StoredPassesMadePerMatch ?? PerMatch(PassesMade, Matches);
    public double? TacklesMadePerMatch => StoredTacklesMadePerMatch ?? PerMatch(TacklesMade, Matches);
    public double? PlayerOfTheMatchRate => StoredPlayerOfTheMatchRate ?? Rate(PlayerOfTheMatch, Matches);
    public double? CleanSheetsDefRate => StoredCleanSheetsDefRate ?? Rate(CleanSheets, Matches);
    public double? CleanSheetsGkRate => StoredCleanSheetsGkRate ?? Rate(CleanSheetsGk, Matches);
    public int? AllClubsGoalContributions => StoredAllClubsGoalContributions ?? (AllClubsGoals is null && AllClubsAssists is null ? null : (AllClubsGoals ?? 0) + (AllClubsAssists ?? 0));
    public double? AllClubsGoalContributionsPerMatch => StoredAllClubsGoalContributionsPerMatch ?? PerMatch(AllClubsGoalContributions, AllClubsMatches);
    public double? AllClubsPlayerOfTheMatchRate => StoredAllClubsPlayerOfTheMatchRate ?? Rate(AllClubsPlayerOfTheMatch, AllClubsMatches);

    private static double? PerMatch(int? value, int? matches) => matches > 0 && value is not null ? Math.Round((double)value.Value / matches.Value, 2) : null;
    private static double? Rate(int? value, int? matches) => matches > 0 && value is not null ? Math.Round((double)value.Value / matches.Value * 100, 2) : null;
}
