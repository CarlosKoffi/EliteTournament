using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class EaApiCacheRepository : IEaApiCacheRepository
{
    private readonly CPEliteDbContext _dbContext;

    public EaApiCacheRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EaApiCacheEntry?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EaApiCacheEntries.FirstOrDefaultAsync(entry => entry.CacheKey == cacheKey, cancellationToken);
    }

    public async Task UpsertAsync(EaApiCacheEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(entry.CacheKey, cancellationToken);
        if (existing is null)
        {
            await _dbContext.EaApiCacheEntries.AddAsync(entry, cancellationToken);
            return;
        }

        existing.Refresh(entry.RawJson, entry.FetchedAt, entry.ExpiresAt);
    }
}
