namespace CPElite.Domain.Entities;

public sealed class EaMatchClubStat
{
    private EaMatchClubStat() { }

    public EaMatchClubStat(Guid id, Guid eaFriendlyMatchId, Guid teamId, long eaClubId, int? goals, int? assists, double? rating, int? shots, int? passesMade, int? passAttempts, int? tacklesMade, int? tackleAttempts, int? saves, int? goalsConceded, int? redCards, int? playerOfTheMatch, int? score, string? result, int? wins, int? losses, int? ties, bool? winnerByDnf, int? regionId, int? eaTeamId, string? stadiumName, string? crestAssetId, string? kitColor1, string? kitColor2, string? kitColor3, string? kitColor4, string rawJson)
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
        Score = score;
        Result = result;
        Wins = wins;
        Losses = losses;
        Ties = ties;
        WinnerByDnf = winnerByDnf;
        RegionId = regionId;
        EaTeamId = eaTeamId;
        StadiumName = stadiumName;
        CrestAssetId = crestAssetId;
        KitColor1 = kitColor1;
        KitColor2 = kitColor2;
        KitColor3 = kitColor3;
        KitColor4 = kitColor4;
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
    public int? Score { get; private set; }
    public string? Result { get; private set; }
    public int? Wins { get; private set; }
    public int? Losses { get; private set; }
    public int? Ties { get; private set; }
    public bool? WinnerByDnf { get; private set; }
    public int? RegionId { get; private set; }
    public int? EaTeamId { get; private set; }
    public string? StadiumName { get; private set; }
    public string? CrestAssetId { get; private set; }
    public string? KitColor1 { get; private set; }
    public string? KitColor2 { get; private set; }
    public string? KitColor3 { get; private set; }
    public string? KitColor4 { get; private set; }
    public string RawJson { get; private set; } = "{}";
    public EaFriendlyMatch? Match { get; private set; }

    public double? PassSuccessRate => PassAttempts > 0 && PassesMade is not null ? Math.Round((double)PassesMade.Value / PassAttempts.Value * 100, 2) : null;
    public double? TackleSuccessRate => TackleAttempts > 0 && TacklesMade is not null ? Math.Round((double)TacklesMade.Value / TackleAttempts.Value * 100, 2) : null;
}
