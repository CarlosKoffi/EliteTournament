namespace CPElite.Contracts.Teams;

public sealed record CreateTeamPositionRequest(string Name, string? Description, int SortOrder);
