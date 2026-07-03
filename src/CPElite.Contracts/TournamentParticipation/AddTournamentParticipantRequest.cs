namespace CPElite.Contracts.TournamentParticipation;

public sealed record AddTournamentParticipantRequest(Guid UserId, string Position, bool IsLoan = false, Guid? LoanFromTeamId = null);
