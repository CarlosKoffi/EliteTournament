using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TournamentRegistrationDraft
{
    private TournamentRegistrationDraft() { }

    public TournamentRegistrationDraft(Guid id, Guid tournamentId, Guid teamId, Guid userId, int step, TournamentPaymentMode paymentMode, bool rulesAccepted, string formation, string selectedPlayersJson, DateTimeOffset createdAt)
    {
        Id = id;
        TournamentId = tournamentId;
        TeamId = teamId;
        UserId = userId;
        CreatedAt = createdAt;
        Refresh(step, paymentMode, rulesAccepted, formation, selectedPlayersJson, createdAt);
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public int Step { get; private set; }
    public TournamentPaymentMode PaymentMode { get; private set; }
    public bool RulesAccepted { get; private set; }
    public string Formation { get; private set; } = "4-2-3-1";
    public string SelectedPlayersJson { get; private set; } = "[]";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Tournament? Tournament { get; private set; }
    public Team? Team { get; private set; }
    public User? User { get; private set; }

    public void Refresh(int step, TournamentPaymentMode paymentMode, bool rulesAccepted, string formation, string selectedPlayersJson, DateTimeOffset updatedAt)
    {
        Step = Math.Clamp(step, 1, 3);
        PaymentMode = paymentMode;
        RulesAccepted = rulesAccepted;
        Formation = string.IsNullOrWhiteSpace(formation) ? "4-2-3-1" : formation.Trim();
        SelectedPlayersJson = string.IsNullOrWhiteSpace(selectedPlayersJson) ? "[]" : selectedPlayersJson;
        UpdatedAt = updatedAt;
    }
}
