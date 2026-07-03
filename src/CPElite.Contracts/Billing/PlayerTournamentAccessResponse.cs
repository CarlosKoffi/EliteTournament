using CPElite.Contracts.Common;

namespace CPElite.Contracts.Billing;

public sealed record PlayerTournamentAccessResponse(Guid UserId, bool HasAccess, TournamentAccessSource Source, Guid? TeamId, string? TeamName);
