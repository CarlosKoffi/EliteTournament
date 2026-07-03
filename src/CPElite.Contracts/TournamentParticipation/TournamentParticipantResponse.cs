using CPElite.Contracts.Common;

namespace CPElite.Contracts.TournamentParticipation;

public sealed record TournamentParticipantResponse(
    Guid Id,
    Guid TournamentId,
    Guid TeamId,
    Guid UserId,
    string DisplayName,
    string? Gamertag,
    string Position,
    TournamentPlayerPresenceStatus Status,
    int? DelayMinutes,
    string? Note,
    bool IsLoan,
    Guid? LoanFromTeamId,
    bool IsLoanApproved,
    DateTimeOffset? LoanApprovedAt,
    DateTimeOffset? LastReminderSentAt,
    bool RequiresOwnerNotification,
    bool ReplacementSuggested,
    DateTimeOffset UpdatedAt);
