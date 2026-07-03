using CPElite.Domain.Entities;

namespace CPElite.Application.Abstractions;

public interface IContentRepository
{
    Task<IReadOnlyCollection<LocalizedContent>> GetByLanguageAsync(string language, CancellationToken cancellationToken = default);
    Task<LocalizedContent?> GetAsync(string key, string language, CancellationToken cancellationToken = default);
    Task AddAsync(LocalizedContent content, CancellationToken cancellationToken = default);
}
