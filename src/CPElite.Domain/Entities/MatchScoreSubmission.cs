using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class MatchScoreSubmission
{
    private MatchScoreSubmission() { }

    public MatchScoreSubmission(Guid id, Guid matchId, Guid submittedByUserId, int homeScore, int awayScore, ScoreVerificationSource source, string? proofUrl, DateTimeOffset submittedAt)
    {
        Id = id;
        MatchId = matchId;
        SubmittedByUserId = submittedByUserId;
        HomeScore = homeScore;
        AwayScore = awayScore;
        Source = source;
        ProofUrl = proofUrl;
        SubmittedAt = submittedAt;
    }

    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public ScoreVerificationSource Source { get; private set; }
    public string? ProofUrl { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public TournamentMatch? Match { get; private set; }
}
