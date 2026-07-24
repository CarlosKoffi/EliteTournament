namespace CPElite.Contracts.Tournaments;

public sealed record TournamentRegistrationDraftPlayerRequest(
    Guid? UserId,
    string EaPlayerId,
    string DisplayName,
    string Position,
    string? AssignedSlotKey);
