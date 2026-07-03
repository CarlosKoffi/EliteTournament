namespace CPElite.Contracts.Tournaments;

public sealed record TournamentRegistrationReplacementResponse(
    Guid TournamentId,
    Guid WithdrawnRegistrationId,
    Guid WithdrawnTeamId,
    string WithdrawnTeamName,
    bool AutoReplacementAttempted,
    Guid? PromotedRegistrationId,
    Guid? PromotedTeamId,
    string? PromotedTeamName,
    string Message,
    TournamentRegistrationSummaryResponse Summary);
