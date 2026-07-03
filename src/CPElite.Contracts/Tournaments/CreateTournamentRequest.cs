using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record CreateTournamentRequest(
    string Name,
    TournamentType Type,
    DateTimeOffset StartsAt,
    string TimeZone,
    int MaxTeams,
    decimal EntryFee,
    string Currency,
    string? GoodiesDescription,
    int MinTeams = 24,
    DateTimeOffset? RegistrationLockAt = null,
    decimal? EstimatedPrizeBudget = null,
    int EaMonitoringStartsMinutesBefore = 15,
    int EaMonitoringEndsMinutesAfter = 60,
    string? PlayerRestrictionsJson = null,
    bool IsCashPrize = false,
    DateTimeOffset? RegistrationStartsAt = null,
    DateTimeOffset? RegistrationEndsAt = null,
    string? BannerUrl = null);
