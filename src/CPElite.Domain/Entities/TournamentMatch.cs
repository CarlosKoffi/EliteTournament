using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TournamentMatch
{
    private TournamentMatch() { }

    public TournamentMatch(Guid id, Guid tournamentId, Guid homeTeamId, Guid awayTeamId, int roundNumber, DateTimeOffset scheduledAt)
    {
        Id = id;
        TournamentId = tournamentId;
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        RoundNumber = roundNumber;
        ScheduledAt = scheduledAt;
        EaLookupFrom = scheduledAt.AddMinutes(-20);
        EaLookupUntil = scheduledAt.AddMinutes(45);
        Status = TournamentMatchStatus.Scheduled;
        Stage = TournamentStage.Group;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid HomeTeamId { get; private set; }
    public Guid AwayTeamId { get; private set; }
    public int RoundNumber { get; private set; }
    public TournamentStage Stage { get; private set; }
    public string? GroupName { get; private set; }
    public int MatchNumber { get; private set; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public DateTimeOffset EaLookupFrom { get; private set; }
    public DateTimeOffset EaLookupUntil { get; private set; }
    public int? HomeScore { get; private set; }
    public int? AwayScore { get; private set; }
    public TournamentMatchStatus Status { get; private set; }
    public Guid? WinnerTeamId { get; private set; }
    public string? EaRawMatchJson { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public Tournament? Tournament { get; private set; }

    public void AssignDrawMetadata(TournamentStage stage, string? groupName, int matchNumber)
    {
        Stage = stage;
        GroupName = groupName;
        MatchNumber = matchNumber;
    }

    public void MarkWaitingForEaData()
    {
        Status = TournamentMatchStatus.WaitingForEaData;
    }

    public void UpdateEaLookupWindow(DateTimeOffset lookupFrom, DateTimeOffset lookupUntil)
    {
        EaLookupFrom = lookupFrom;
        EaLookupUntil = lookupUntil;
    }

    public void RequireOwnerConfirmation()
    {
        Status = TournamentMatchStatus.OwnerConfirmationRequired;
    }

    public void CaptureEaCandidateScore(int homeScore, int awayScore, Guid? winnerTeamId, string eaRawMatchJson, DateTimeOffset capturedAt)
    {
        HomeScore = homeScore;
        AwayScore = awayScore;
        WinnerTeamId = winnerTeamId;
        EaRawMatchJson = eaRawMatchJson;
        VerifiedAt = capturedAt;
        Status = TournamentMatchStatus.OwnerConfirmationRequired;
    }

    public void ApplyVerifiedScore(int homeScore, int awayScore, Guid? winnerTeamId, ScoreVerificationSource source, string? eaRawMatchJson, DateTimeOffset verifiedAt)
    {
        HomeScore = homeScore;
        AwayScore = awayScore;
        WinnerTeamId = winnerTeamId;
        EaRawMatchJson = eaRawMatchJson;
        VerifiedAt = verifiedAt;
        Status = source == ScoreVerificationSource.EaApi ? TournamentMatchStatus.AutoVerified : TournamentMatchStatus.ScoreSubmitted;
    }
}
