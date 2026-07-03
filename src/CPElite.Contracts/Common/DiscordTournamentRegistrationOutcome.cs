namespace CPElite.Contracts.Common;

public enum DiscordTournamentRegistrationOutcome
{
    Registered = 1,
    Waitlisted = 2,
    AlreadyRegistered = 3,
    TeamNotFound = 4,
    TournamentClosed = 5,
    TournamentCancelled = 6,
    MinimumNotReached = 7,
    Locked = 8,
    NoShowMarked = 9,
    PromotedFromWaitlist = 10
}
