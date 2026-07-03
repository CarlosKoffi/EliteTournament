namespace CPElite.Domain.Entities;

public sealed class ChampionTitle
{
    private ChampionTitle() { }

    public ChampionTitle(Guid id, Guid teamId, Guid tournamentId, DateTimeOffset crownedAt, decimal nextEntryDiscountAmount, string currency)
    {
        Id = id;
        TeamId = teamId;
        TournamentId = tournamentId;
        CrownedAt = crownedAt;
        NextEntryDiscountAmount = nextEntryDiscountAmount;
        Currency = currency;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid TournamentId { get; private set; }
    public DateTimeOffset CrownedAt { get; private set; }
    public DateTimeOffset? DethronedAt { get; private set; }
    public bool IsActive { get; private set; }
    public decimal NextEntryDiscountAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public Team? Team { get; private set; }

    public void Dethrone(DateTimeOffset dethronedAt)
    {
        IsActive = false;
        DethronedAt = dethronedAt;
    }
}
