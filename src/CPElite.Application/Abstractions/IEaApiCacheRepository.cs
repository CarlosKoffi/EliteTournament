using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface IEaApiCacheRepository
{
    Task<EaApiCacheEntry?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task UpsertAsync(EaApiCacheEntry entry, CancellationToken cancellationToken = default);
}
