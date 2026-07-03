using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record RegisterTeamForTournamentRequest(Guid TeamId, TournamentPaymentMode PaymentMode);
