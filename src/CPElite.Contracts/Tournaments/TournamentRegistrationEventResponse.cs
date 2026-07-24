using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentRegistrationEventResponse(
    Guid Id,
    Guid TournamentId,
    Guid TeamId,
    string? TeamName,
    Guid? TournamentRegistrationId,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string EventType,
    string Step,
    TournamentRegistrationStatus? RegistrationStatus,
    TournamentPaymentMode? PaymentMode,
    string Message,
    string? PayloadJson,
    DateTimeOffset CreatedAt);
