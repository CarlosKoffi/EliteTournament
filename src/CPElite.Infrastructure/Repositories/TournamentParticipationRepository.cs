using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class TournamentParticipationRepository : ITournamentParticipationRepository
{
    private readonly CPEliteDbContext _dbContext;

    public TournamentParticipationRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TournamentPlayerConfirmation?> GetAsync(Guid tournamentId, Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentPlayerConfirmations
            .Include(confirmation => confirmation.User)
            .FirstOrDefaultAsync(confirmation => confirmation.TournamentId == tournamentId && confirmation.TeamId == teamId && confirmation.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TournamentPlayerConfirmation>> GetTeamConfirmationsAsync(Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TournamentPlayerConfirmations
            .Include(confirmation => confirmation.User)
            .Where(confirmation => confirmation.TournamentId == tournamentId && confirmation.TeamId == teamId)
            .OrderBy(confirmation => confirmation.Position)
            .ThenBy(confirmation => confirmation.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(TournamentPlayerConfirmation confirmation, CancellationToken cancellationToken = default)
    {
        await _dbContext.TournamentPlayerConfirmations.AddAsync(confirmation, cancellationToken);
    }
}
