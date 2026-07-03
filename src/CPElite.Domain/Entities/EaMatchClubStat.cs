namespace CPElite.Domain.Entities;

public sealed class EaMatchClubStat
{
    private EaMatchClubStat() { }

    public EaMatchClubStat(Guid id, Guid eaFriendlyMatchId, Guid teamId, long eaClubId, int? goals, int? assists, double? rating, int? shots, int? passesMade, int? passAttempts, int? tacklesMade, int? tackleAttempts, int? saves, int? goalsConceded, int? redCards, int? playerOfTheMatch, string rawJson)
    {
        Id = id;
        EaFriendlyMatchId = eaFriendlyMatchId;
        TeamId = teamId;
        EaClubId = eaClubId;
        Goals = goals;
        Assists = assists;
        Rating = rating;
        Shots = shots;
        PassesMade = passesMade;
        PassAttempts = passAttempts;
        TacklesMade = tacklesMade;
        TackleAttempts = tackleAttempts;
        Saves = saves;
        GoalsConceded = goalsConceded;
        RedCards = redCards;
        PlayerOfTheMatch = playerOfTheMatch;
        RawJson = rawJson;
    }

    public Guid Id { get; private set; }
    public Guid EaFriendlyMatchId { get; private set; }
    public Guid TeamId { get; private set; }
    public long EaClubId { get; private set; }
    public int? Goals { get; private set; }
    public int? Assists { get; private set; }
    public double? Rating { get; private set; }
    public int? Shots { get; private set; }
    public int? PassesMade { get; private set; }
    public int? PassAttempts { get; private set; }
    public int? TacklesMade { get; private set; }
    public int? TackleAttempts { get; private set; }
    public int? Saves { get; private set; }
    public int? GoalsConceded { get; private set; }
    public int? RedCards { get; private set; }
    public int? PlayerOfTheMatch { get; private set; }
    public string RawJson { get; private set; } = "{}";
    public EaFriendlyMatch? Match { get; private set; }

    public double? PassSuccessRate => PassAttempts > 0 && PassesMade is not null ? Math.Round((double)PassesMade.Value / PassAttempts.Value * 100, 2) : null;
    public double? TackleSuccessRate => TackleAttempts > 0 && TacklesMade is not null ? Math.Round((double)TacklesMade.Value / TackleAttempts.Value * 100, 2) : null;
}
