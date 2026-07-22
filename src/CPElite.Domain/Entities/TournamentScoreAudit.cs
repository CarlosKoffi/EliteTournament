using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TournamentScoreAudit
{
    private TournamentScoreAudit() { }

    public TournamentScoreAudit(
        Guid id,
        Guid tournamentId,
        Guid tournamentMatchId,
        DateTimeOffset attemptedAt,
        string trigger,
        ScoreReconciliationStatus status,
        string summary,
        string? eaMatchId,
        int candidateCount,
        int? confidence,
        int? homeScore,
        int? awayScore,
        int? manualHomeScore,
        int? manualAwayScore,
        bool teamsMatched,
        bool timestampMatched,
        bool scoreMatched,
        bool playersMatched,
        bool durationLooksComplete,
        bool duplicateCandidateDetected,
        string issuesJson,
        string evidenceJson,
        string? rawCandidateJson)
    {
        Id = id;
        TournamentId = tournamentId;
        TournamentMatchId = tournamentMatchId;
        AttemptedAt = attemptedAt;
        Trigger = trigger;
        Status = status;
        Summary = summary;
        EaMatchId = eaMatchId;
        CandidateCount = candidateCount;
        Confidence = confidence;
        HomeScore = homeScore;
        AwayScore = awayScore;
        ManualHomeScore = manualHomeScore;
        ManualAwayScore = manualAwayScore;
        TeamsMatched = teamsMatched;
        TimestampMatched = timestampMatched;
        ScoreMatched = scoreMatched;
        PlayersMatched = playersMatched;
        DurationLooksComplete = durationLooksComplete;
        DuplicateCandidateDetected = duplicateCandidateDetected;
        IssuesJson = issuesJson;
        EvidenceJson = evidenceJson;
        RawCandidateJson = rawCandidateJson;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid TournamentMatchId { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }
    public string Trigger { get; private set; } = string.Empty;
    public ScoreReconciliationStatus Status { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? EaMatchId { get; private set; }
    public int CandidateCount { get; private set; }
    public int? Confidence { get; private set; }
    public int? HomeScore { get; private set; }
    public int? AwayScore { get; private set; }
    public int? ManualHomeScore { get; private set; }
    public int? ManualAwayScore { get; private set; }
    public bool TeamsMatched { get; private set; }
    public bool TimestampMatched { get; private set; }
    public bool ScoreMatched { get; private set; }
    public bool PlayersMatched { get; private set; }
    public bool DurationLooksComplete { get; private set; }
    public bool DuplicateCandidateDetected { get; private set; }
    public string IssuesJson { get; private set; } = "[]";
    public string EvidenceJson { get; private set; } = "{}";
    public string? RawCandidateJson { get; private set; }
    public Tournament? Tournament { get; private set; }
    public TournamentMatch? TournamentMatch { get; private set; }
}
