using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class Tournament
{
    private Tournament() { }

    public Tournament(Guid id, string name, TournamentType type, DateTimeOffset startsAt, string timeZone, int minTeams, int maxTeams, decimal entryFee, string currency, string? goodiesDescription, DateTimeOffset? registrationLockAt, decimal? estimatedPrizeBudget, int eaMonitoringStartsMinutesBefore, int eaMonitoringEndsMinutesAfter, string? playerRestrictionsJson, DateTimeOffset createdAt, Guid createdByUserId, bool isCashPrize = false, DateTimeOffset? registrationStartsAt = null, DateTimeOffset? registrationEndsAt = null, string? bannerUrl = null, TournamentScoreRecoveryMode scoreRecoveryMode = TournamentScoreRecoveryMode.ManualOnly, int scoreRecoveryIntervalMinutes = 2, bool autoPublishPerfectScore = false)
    {
        Id = id;
        Name = name;
        Type = type;
        StartsAt = startsAt;
        TimeZone = timeZone;
        MinTeams = minTeams;
        MaxTeams = maxTeams;
        EntryFee = entryFee;
        Currency = currency;
        GoodiesDescription = goodiesDescription;
        RegistrationLockAt = registrationLockAt;
        RegistrationStartsAt = registrationStartsAt;
        RegistrationEndsAt = registrationEndsAt;
        EstimatedPrizeBudget = estimatedPrizeBudget;
        EaMonitoringStartsMinutesBefore = eaMonitoringStartsMinutesBefore;
        EaMonitoringEndsMinutesAfter = eaMonitoringEndsMinutesAfter;
        PlayerRestrictionsJson = playerRestrictionsJson;
        Status = TournamentStatus.Draft;
        CreatedAt = createdAt;
        CreatedByUserId = createdByUserId;
        IsCashPrize = isCashPrize;
        BannerUrl = bannerUrl;
        ScoreRecoveryMode = scoreRecoveryMode;
        ScoreRecoveryIntervalMinutes = scoreRecoveryIntervalMinutes;
        AutoPublishPerfectScore = autoPublishPerfectScore;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TournamentType Type { get; private set; }
    public TournamentStatus Status { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public string TimeZone { get; private set; } = "Europe/Zurich";
    public int MinTeams { get; private set; }
    public int MaxTeams { get; private set; }
    public decimal EntryFee { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public string? GoodiesDescription { get; private set; }
    public DateTimeOffset? RegistrationLockAt { get; private set; }
    public DateTimeOffset? RegistrationStartsAt { get; private set; }
    public DateTimeOffset? RegistrationEndsAt { get; private set; }
    public decimal? EstimatedPrizeBudget { get; private set; }
    public bool IsCashPrize { get; private set; }
    public int EaMonitoringStartsMinutesBefore { get; private set; } = 15;
    public int EaMonitoringEndsMinutesAfter { get; private set; } = 60;
    public string? PlayerRestrictionsJson { get; private set; }
    public string? BannerUrl { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public TournamentScoreRecoveryMode ScoreRecoveryMode { get; private set; } = TournamentScoreRecoveryMode.ManualOnly;
    public int ScoreRecoveryIntervalMinutes { get; private set; } = 2;
    public bool AutoPublishPerfectScore { get; private set; }

    public void OpenRegistration()
    {
        Status = TournamentStatus.RegistrationOpen;
    }

    public void LockRegistration()
    {
        Status = TournamentStatus.RegistrationLocked;
    }

    public void Cancel()
    {
        Status = TournamentStatus.Cancelled;
    }

    public void Start()
    {
        Status = TournamentStatus.InProgress;
    }
}
