using CPElite.Contracts.Common;

namespace CPElite.Contracts.TournamentParticipation;

public sealed record UpdateMyTournamentPresenceRequest(TournamentPlayerPresenceStatus Status, int? DelayMinutes = null, string? Note = null);
