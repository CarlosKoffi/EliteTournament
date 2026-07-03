using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentMatchResponse(
    Guid Id,
    Guid TournamentId,
    Guid HomeTeamId,
    Guid AwayTeamId,
    int RoundNumber,
    DateTimeOffset ScheduledAt,
    DateTimeOffset EaLookupFrom,
    DateTimeOffset EaLookupUntil,
    int? HomeScore,
    int? AwayScore,
    TournamentMatchStatus Status,
    Guid? WinnerTeamId,
    TournamentStage Stage = TournamentStage.Group,
    string? GroupName = null,
    int MatchNumber = 0);
