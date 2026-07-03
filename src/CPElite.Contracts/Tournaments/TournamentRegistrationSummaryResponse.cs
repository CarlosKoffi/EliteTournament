namespace CPElite.Contracts.Tournaments;

public sealed record TournamentRegistrationSummaryResponse(
    Guid TournamentId,
    string TournamentName,
    DateTimeOffset StartsAt,
    DateTimeOffset? RegistrationStartsAt,
    DateTimeOffset? RegistrationEndsAt,
    DateTimeOffset? RegistrationLockAt,
    int MinTeams,
    int MaxTeams,
    decimal EntryFee,
    string Currency,
    int ActiveTeams,
    int PendingTeams,
    int AcceptedTeams,
    int WaitlistedTeams,
    int NoShowTeams,
    bool IsMinimumReached,
    bool IsFull,
    bool IsLocked,
    IReadOnlyCollection<TournamentRegistrationListItemResponse> Teams);
