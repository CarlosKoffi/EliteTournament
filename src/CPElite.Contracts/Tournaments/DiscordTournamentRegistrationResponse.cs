using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record DiscordTournamentRegistrationResponse(
    DiscordTournamentRegistrationOutcome Outcome,
    string Message,
    Guid? TournamentId,
    Guid? TeamId,
    string? TeamName,
    Guid? RegistrationId,
    TournamentRegistrationStatus? RegistrationStatus,
    bool RequiresAppAction,
    TournamentRegistrationSummaryResponse Summary);
