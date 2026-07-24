using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface ITournamentRepository
{
    Task<IReadOnlyCollection<Tournament>> GetTournamentsAsync(CancellationToken cancellationToken = default);
    Task<Tournament?> GetTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<TournamentMatch?> GetMatchAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentMatch>> GetMatchesAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentRegistration>> GetTeamRegistrationsAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentMatch>> GetTeamMatchesAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChampionTitle>> GetTeamChampionTitlesAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentMatch>> GetEaVerificationDueMatchesAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentScoreAudit>> GetScoreAuditsAsync(Guid tournamentId, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentRegistrationEvent>> GetRegistrationEventsAsync(Guid tournamentId, Guid? teamId = null, int take = 200, CancellationToken cancellationToken = default);
    Task<TournamentRegistration?> GetRegistrationAsync(Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentRegistration>> GetRegistrationsAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task AddTournamentAsync(Tournament tournament, CancellationToken cancellationToken = default);
    Task AddRegistrationAsync(TournamentRegistration registration, CancellationToken cancellationToken = default);
    Task AddMatchAsync(TournamentMatch match, CancellationToken cancellationToken = default);
    Task AddScoreSubmissionAsync(MatchScoreSubmission submission, CancellationToken cancellationToken = default);
    Task AddChampionTitleAsync(ChampionTitle championTitle, CancellationToken cancellationToken = default);
    Task AddMomentAsync(TournamentMoment moment, CancellationToken cancellationToken = default);
    Task AddScoreAuditAsync(TournamentScoreAudit audit, CancellationToken cancellationToken = default);
    Task AddRegistrationEventAsync(TournamentRegistrationEvent registrationEvent, CancellationToken cancellationToken = default);
    Task<TournamentMoment?> GetMomentAsync(Guid momentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentMoment>> GetPendingDiscordMomentsAsync(int take, CancellationToken cancellationToken = default);
}
