using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record EaMatchVerificationResponse(
    Guid MatchId,
    TournamentMatchStatus Status,
    bool AutoVerified,
    string Message,
    int? Confidence = null,
    int? HomeScore = null,
    int? AwayScore = null,
    bool RequiresOwnerValidation = false,
    string? Evidence = null);
