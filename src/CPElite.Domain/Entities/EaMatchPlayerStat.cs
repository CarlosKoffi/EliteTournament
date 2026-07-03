namespace CPElite.Domain.Entities;

public sealed class EaMatchPlayerStat
{
    private EaMatchPlayerStat() { }

    public EaMatchPlayerStat(Guid id, Guid eaFriendlyMatchId, Guid teamId, long eaClubId, string eaPlayerId, string playerName, string? position, int? goals, int? assists, double? rating, bool playerOfTheMatch, int? shots, int? passesMade, int? passAttempts, int? tacklesMade, int? tackleAttempts, int? saves, int? goalsConceded, int? secondsPlayed, string? vproAttributes, int? cleanSheetsAny, int? cleanSheetsDef, int? cleanSheetsGk, int? ballDiveSaves, int? crossSaves, int? goodDirectionSaves, int? parrySaves, int? punchSaves, int? reflexSaves, int? redCards, int? gameTime, int? realtimeGame, int? realtimeIdle, int? archetypeId, string? matchEventAggregate0, string? matchEventAggregate1, string? matchEventAggregate2, string? matchEventAggregate3, string rawJson)
    {
        Id = id;
        EaFriendlyMatchId = eaFriendlyMatchId;
        TeamId = teamId;
        EaClubId = eaClubId;
        EaPlayerId = eaPlayerId;
        PlayerName = playerName;
        Position = position;
        Goals = goals;
        Assists = assists;
        Rating = rating;
        PlayerOfTheMatch = playerOfTheMatch;
        Shots = shots;
        PassesMade = passesMade;
        PassAttempts = passAttempts;
        TacklesMade = tacklesMade;
        TackleAttempts = tackleAttempts;
        Saves = saves;
        GoalsConceded = goalsConceded;
        SecondsPlayed = secondsPlayed;
        VproAttributes = vproAttributes;
        CleanSheetsAny = cleanSheetsAny;
        CleanSheetsDef = cleanSheetsDef;
        CleanSheetsGk = cleanSheetsGk;
        BallDiveSaves = ballDiveSaves;
        CrossSaves = crossSaves;
        GoodDirectionSaves = goodDirectionSaves;
        ParrySaves = parrySaves;
        PunchSaves = punchSaves;
        ReflexSaves = reflexSaves;
        RedCards = redCards;
        GameTime = gameTime;
        RealtimeGame = realtimeGame;
        RealtimeIdle = realtimeIdle;
        ArchetypeId = archetypeId;
        MatchEventAggregate0 = matchEventAggregate0;
        MatchEventAggregate1 = matchEventAggregate1;
        MatchEventAggregate2 = matchEventAggregate2;
        MatchEventAggregate3 = matchEventAggregate3;
        RawJson = rawJson;
    }

    public Guid Id { get; private set; }
    public Guid EaFriendlyMatchId { get; private set; }
    public Guid TeamId { get; private set; }
    public long EaClubId { get; private set; }
    public string EaPlayerId { get; private set; } = string.Empty;
    public string PlayerName { get; private set; } = string.Empty;
    public string? Position { get; private set; }
    public int? Goals { get; private set; }
    public int? Assists { get; private set; }
    public double? Rating { get; private set; }
    public bool PlayerOfTheMatch { get; private set; }
    public int? Shots { get; private set; }
    public int? PassesMade { get; private set; }
    public int? PassAttempts { get; private set; }
    public int? TacklesMade { get; private set; }
    public int? TackleAttempts { get; private set; }
    public int? Saves { get; private set; }
    public int? GoalsConceded { get; private set; }
    public int? SecondsPlayed { get; private set; }
    public string? VproAttributes { get; private set; }
    public int? CleanSheetsAny { get; private set; }
    public int? CleanSheetsDef { get; private set; }
    public int? CleanSheetsGk { get; private set; }
    public int? BallDiveSaves { get; private set; }
    public int? CrossSaves { get; private set; }
    public int? GoodDirectionSaves { get; private set; }
    public int? ParrySaves { get; private set; }
    public int? PunchSaves { get; private set; }
    public int? ReflexSaves { get; private set; }
    public int? RedCards { get; private set; }
    public int? GameTime { get; private set; }
    public int? RealtimeGame { get; private set; }
    public int? RealtimeIdle { get; private set; }
    public int? ArchetypeId { get; private set; }
    public string? MatchEventAggregate0 { get; private set; }
    public string? MatchEventAggregate1 { get; private set; }
    public string? MatchEventAggregate2 { get; private set; }
    public string? MatchEventAggregate3 { get; private set; }
    public string RawJson { get; private set; } = "{}";
    public EaFriendlyMatch? Match { get; private set; }

    public double? PassSuccessRate => PassAttempts > 0 && PassesMade is not null ? Math.Round((double)PassesMade.Value / PassAttempts.Value * 100, 2) : null;
    public double? TackleSuccessRate => TackleAttempts > 0 && TacklesMade is not null ? Math.Round((double)TacklesMade.Value / TackleAttempts.Value * 100, 2) : null;
}
