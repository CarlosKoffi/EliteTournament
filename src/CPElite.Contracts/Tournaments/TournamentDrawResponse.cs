namespace CPElite.Contracts.Tournaments;

public sealed record TournamentDrawResponse(
    Guid TournamentId,
    string TournamentName,
    int TeamCount,
    int GroupCount,
    int QualifiersPerGroup,
    IReadOnlyCollection<TournamentGroupResponse> Groups,
    IReadOnlyCollection<TournamentMatchResponse> GroupMatches,
    IReadOnlyCollection<TournamentKnockoutRoundPlanResponse> KnockoutPlan);
