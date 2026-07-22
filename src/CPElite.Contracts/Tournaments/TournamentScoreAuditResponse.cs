using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentScoreAuditResponse(
    Guid Id,
    Guid TournamentId,
    Guid MatchId,
    DateTimeOffset AttemptedAt,
    string Trigger,
    ScoreReconciliationStatus Status,
    string Summary,
    string? EaMatchId,
    int CandidateCount,
    int? Confidence,
    int? HomeScore,
    int? AwayScore,
    int? ManualHomeScore,
    int? ManualAwayScore,
    bool TeamsMatched,
    bool TimestampMatched,
    bool ScoreMatched,
    bool PlayersMatched,
    bool DurationLooksComplete,
    bool DuplicateCandidateDetected,
    string IssuesJson,
    string EvidenceJson);
