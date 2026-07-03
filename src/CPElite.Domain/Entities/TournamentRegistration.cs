using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TournamentRegistration
{
    private TournamentRegistration() { }

    public TournamentRegistration(Guid id, Guid tournamentId, Guid teamId, Guid registeredByUserId, TournamentPaymentMode paymentMode, DateTimeOffset createdAt, string source = "App", string? discordGuildId = null, string? discordChannelId = null, string? discordMessageId = null, string? discordRequestedByUserId = null)
    {
        Id = id;
        TournamentId = tournamentId;
        TeamId = teamId;
        RegisteredByUserId = registeredByUserId;
        PaymentMode = paymentMode;
        Status = TournamentRegistrationStatus.Pending;
        CreatedAt = createdAt;
        Source = source;
        DiscordGuildId = discordGuildId;
        DiscordChannelId = discordChannelId;
        DiscordMessageId = discordMessageId;
        DiscordRequestedByUserId = discordRequestedByUserId;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid RegisteredByUserId { get; private set; }
    public TournamentPaymentMode PaymentMode { get; private set; }
    public TournamentRegistrationStatus Status { get; private set; }
    public bool IsPaymentComplete { get; private set; }
    public string Source { get; private set; } = "App";
    public string? DiscordGuildId { get; private set; }
    public string? DiscordChannelId { get; private set; }
    public string? DiscordMessageId { get; private set; }
    public string? DiscordRequestedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Tournament? Tournament { get; private set; }
    public Team? Team { get; private set; }

    public void Accept()
    {
        Status = TournamentRegistrationStatus.Accepted;
    }

    public void Waitlist()
    {
        Status = TournamentRegistrationStatus.Waitlisted;
    }

    public void MarkNoShow()
    {
        Status = TournamentRegistrationStatus.NoShow;
    }

    public void Withdraw()
    {
        Status = TournamentRegistrationStatus.Withdrawn;
    }

    public void MarkPaymentComplete()
    {
        IsPaymentComplete = true;
    }
}
