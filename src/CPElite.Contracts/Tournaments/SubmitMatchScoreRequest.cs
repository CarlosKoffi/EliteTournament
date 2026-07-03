namespace CPElite.Contracts.Tournaments;

public sealed record SubmitMatchScoreRequest(int HomeScore, int AwayScore, string? ProofUrl);
