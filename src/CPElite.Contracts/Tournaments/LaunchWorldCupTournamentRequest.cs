namespace CPElite.Contracts.Tournaments;

public sealed record LaunchWorldCupTournamentRequest(
    int GroupSize = 4,
    int QualifiersPerGroup = 2,
    int MinutesBetweenGroupMatches = 20,
    int? DrawSeed = null);
