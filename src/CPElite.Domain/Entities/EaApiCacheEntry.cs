namespace CPElite.Domain.Entities;

public sealed class EaApiCacheEntry
{
    private EaApiCacheEntry() { }

    public EaApiCacheEntry(Guid id, string cacheKey, string endpoint, string rawJson, DateTimeOffset fetchedAt, DateTimeOffset expiresAt)
    {
        Id = id;
        CacheKey = cacheKey;
        Endpoint = endpoint;
        RawJson = rawJson;
        FetchedAt = fetchedAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public string CacheKey { get; private set; } = string.Empty;
    public string Endpoint { get; private set; } = string.Empty;
    public string RawJson { get; private set; } = string.Empty;
    public DateTimeOffset FetchedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public bool IsFresh(DateTimeOffset now)
    {
        return ExpiresAt > now;
    }

    public void Refresh(string rawJson, DateTimeOffset fetchedAt, DateTimeOffset expiresAt)
    {
        RawJson = rawJson;
        FetchedAt = fetchedAt;
        ExpiresAt = expiresAt;
    }
}
