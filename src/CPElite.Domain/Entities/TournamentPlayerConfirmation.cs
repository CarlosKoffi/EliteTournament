using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TournamentPlayerConfirmation
{
    private TournamentPlayerConfirmation() { }

    public TournamentPlayerConfirmation(Guid id, Guid tournamentId, Guid teamId, Guid userId, string position, bool isLoan, Guid? loanFromTeamId, DateTimeOffset createdAt)
    {
        Id = id;
        TournamentId = tournamentId;
        TeamId = teamId;
        UserId = userId;
        Position = position;
        IsLoan = isLoan;
        LoanFromTeamId = loanFromTeamId;
        Status = TournamentPlayerPresenceStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public string Position { get; private set; } = string.Empty;
    public TournamentPlayerPresenceStatus Status { get; private set; }
    public int? DelayMinutes { get; private set; }
    public string? Note { get; private set; }
    public bool IsLoan { get; private set; }
    public Guid? LoanFromTeamId { get; private set; }
    public DateTimeOffset? LoanApprovedAt { get; private set; }
    public Guid? LoanApprovedByUserId { get; private set; }
    public DateTimeOffset? LastReminderSentAt { get; private set; }
    public bool RequiresOwnerNotification { get; private set; }
    public bool ReplacementSuggested { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Tournament? Tournament { get; private set; }
    public Team? Team { get; private set; }
    public User? User { get; private set; }

    public void UpdatePresence(TournamentPlayerPresenceStatus status, int? delayMinutes, string? note, DateTimeOffset updatedAt)
    {
        Status = status;
        DelayMinutes = status == TournamentPlayerPresenceStatus.Late ? delayMinutes : null;
        Note = note;
        RequiresOwnerNotification = status is TournamentPlayerPresenceStatus.Late or TournamentPlayerPresenceStatus.Unavailable;
        ReplacementSuggested = status == TournamentPlayerPresenceStatus.Unavailable;
        UpdatedAt = updatedAt;
    }

    public void ApproveLoan(Guid approvedByUserId, DateTimeOffset approvedAt)
    {
        LoanApprovedByUserId = approvedByUserId;
        LoanApprovedAt = approvedAt;
        UpdatedAt = approvedAt;
    }

    public void MarkReminderSent(DateTimeOffset sentAt)
    {
        LastReminderSentAt = sentAt;
        UpdatedAt = sentAt;
    }
}
