using CPElite.Contracts.Common;

namespace CPElite.Contracts.Teams;

public sealed record TeamTournamentSummaryResponse(
    Guid TeamId,
    int TournamentsPlayed,
    int Titles,
    int Finals,
    int Podiums,
    int Matches,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int CleanSheets,
    IReadOnlyCollection<TeamTournamentHistoryItemResponse> RecentTournaments,
    IReadOnlyCollection<TeamTournamentMatchSummaryResponse> RecentMatches);

public sealed record TeamTournamentHistoryItemResponse(
    Guid TournamentId,
    string Name,
    TournamentType Type,
    TournamentStatus Status,
    TournamentTier Tier,
    DateTimeOffset StartsAt,
    bool IsChampion,
    string FinishLabel,
    int Matches,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst);

public sealed record TeamTournamentMatchSummaryResponse(
    Guid MatchId,
    Guid TournamentId,
    string TournamentName,
    TournamentStage Stage,
    string? GroupName,
    int MatchNumber,
    DateTimeOffset ScheduledAt,
    Guid HomeTeamId,
    Guid AwayTeamId,
    string? HomeTeamName,
    string? AwayTeamName,
    int? HomeScore,
    int? AwayScore,
    TournamentMatchStatus Status,
    Guid? WinnerTeamId,
    bool IsWin,
    bool IsDraw,
    int GoalsFor,
    int GoalsAgainst);
