namespace CPElite.Contracts.Tournaments;

public sealed record TournamentGroupResponse(string Name, IReadOnlyCollection<TournamentGroupTeamResponse> Teams);
