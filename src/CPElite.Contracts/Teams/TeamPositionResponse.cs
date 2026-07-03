namespace CPElite.Contracts.Teams;

public sealed record TeamPositionResponse(Guid Id, string Name, string? Description, int SortOrder);
