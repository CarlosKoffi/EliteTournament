namespace CPElite.Contracts.TournamentParticipation;

public sealed record TournamentParticipantReminderResponse(Guid TournamentId, Guid TeamId, int PendingPlayers, DateTimeOffset ReminderSentAt);
