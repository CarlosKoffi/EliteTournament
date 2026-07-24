using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TournamentRegistrationEvent
{
    private TournamentRegistrationEvent() { }

    public TournamentRegistrationEvent(
        Guid id,
        Guid tournamentId,
        Guid teamId,
        Guid? tournamentRegistrationId,
        Guid? actorUserId,
        string eventType,
        string step,
        TournamentRegistrationStatus? registrationStatus,
        TournamentPaymentMode? paymentMode,
        string message,
        DateTimeOffset createdAt,
        string? payloadJson = null)
    {
        Id = id;
        TournamentId = tournamentId;
        TeamId = teamId;
        TournamentRegistrationId = tournamentRegistrationId;
        ActorUserId = actorUserId;
        EventType = eventType.Trim();
        Step = step.Trim();
        RegistrationStatus = registrationStatus;
        PaymentMode = paymentMode;
        Message = message.Trim();
        CreatedAt = createdAt;
        PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid? TournamentRegistrationId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Step { get; private set; } = string.Empty;
    public TournamentRegistrationStatus? RegistrationStatus { get; private set; }
    public TournamentPaymentMode? PaymentMode { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? PayloadJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Tournament? Tournament { get; private set; }
    public Team? Team { get; private set; }
    public TournamentRegistration? TournamentRegistration { get; private set; }
    public User? ActorUser { get; private set; }
}
