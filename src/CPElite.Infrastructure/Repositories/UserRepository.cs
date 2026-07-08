using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly CPEliteDbContext _dbContext;

    public UserRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByEaIdentityAsync(string eaPlayerId, string playerName, CancellationToken cancellationToken = default)
    {
        var candidates = new[] { eaPlayerId, playerName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        if (candidates.Length == 0)
        {
            return null;
        }

        return await _dbContext.Users.FirstOrDefaultAsync(user =>
            (user.EaSportsId != null && candidates.Contains(user.EaSportsId.ToUpper())) ||
            (user.Gamertag != null && candidates.Contains(user.Gamertag.ToUpper())) ||
            candidates.Contains(user.DisplayName.ToUpper()), cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }
}
