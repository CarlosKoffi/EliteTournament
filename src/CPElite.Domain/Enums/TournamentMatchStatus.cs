namespace CPElite.Domain.Enums;

public enum TournamentMatchStatus
{
    Scheduled = 1,
    WaitingForEaData = 2,
    OwnerConfirmationRequired = 3,
    ScoreSubmitted = 4,
    AutoVerified = 5,
    Disputed = 6,
    Completed = 7
}
