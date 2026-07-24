using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record SaveTournamentRegistrationDraftRequest(
    Guid TeamId,
    int Step,
    TournamentPaymentMode PaymentMode,
    bool RulesAccepted,
    string Formation,
    IReadOnlyCollection<TournamentRegistrationDraftPlayerRequest> SelectedPlayers);
