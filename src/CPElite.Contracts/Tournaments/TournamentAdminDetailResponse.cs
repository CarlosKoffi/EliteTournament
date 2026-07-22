namespace CPElite.Contracts.Tournaments;

public sealed record TournamentAdminDetailResponse(
    TournamentResponse Tournament,
    TournamentRegistrationSummaryResponse RegistrationSummary,
    IReadOnlyCollection<TournamentMatchResponse> Matches,
    IReadOnlyCollection<TournamentScoreAuditResponse> ScoreAudits);
