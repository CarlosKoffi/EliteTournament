using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface ITournamentParticipationRepository
{
    Task<TournamentPlayerConfirmation?> GetAsync(Guid tournamentId, Guid teamId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentPlayerConfirmation>> GetTeamConfirmationsAsync(Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default);
    Task AddAsync(TournamentPlayerConfirmation confirmation, CancellationToken cancellationToken = default);
}
