using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentRegistrationDraftResponse(
    Guid Id,
    Guid TournamentId,
    Guid TeamId,
    Guid UserId,
    int Step,
    TournamentPaymentMode PaymentMode,
    bool RulesAccepted,
    string Formation,
    IReadOnlyCollection<TournamentRegistrationDraftPlayerRequest> SelectedPlayers,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
