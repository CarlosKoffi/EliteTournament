using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentResponse(
    Guid Id,
    string Name,
    TournamentType Type,
    TournamentStatus Status,
    DateTimeOffset StartsAt,
    string TimeZone,
    int MinTeams,
    int MaxTeams,
    decimal EntryFee,
    string Currency,
    string? GoodiesDescription,
    DateTimeOffset? RegistrationLockAt,
    decimal? EstimatedPrizeBudget,
    int EaMonitoringStartsMinutesBefore,
    int EaMonitoringEndsMinutesAfter,
    string? PlayerRestrictionsJson,
    bool IsCashPrize = false,
    DateTimeOffset? RegistrationStartsAt = null,
    DateTimeOffset? RegistrationEndsAt = null,
    string? BannerUrl = null,
    TournamentScoreRecoveryMode ScoreRecoveryMode = TournamentScoreRecoveryMode.ManualOnly,
    int ScoreRecoveryIntervalMinutes = 2,
    bool AutoPublishPerfectScore = false);
