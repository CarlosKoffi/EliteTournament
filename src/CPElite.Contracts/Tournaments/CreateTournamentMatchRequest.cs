namespace CPElite.Contracts.Tournaments;

public sealed record CreateTournamentMatchRequest(Guid HomeTeamId, Guid AwayTeamId, int RoundNumber, DateTimeOffset ScheduledAt);
