using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Infrastructure.Repositories;

public sealed class ContentRepository : IContentRepository
{
    private readonly CPEliteDbContext _dbContext;

    public ContentRepository(CPEliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<LocalizedContent>> GetByLanguageAsync(string language, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LocalizedContents
            .AsNoTracking()
            .Where(content => content.Language == language)
            .OrderBy(content => content.Section)
            .ThenBy(content => content.Key)
            .ToArrayAsync(cancellationToken);
    }

    public Task<LocalizedContent?> GetAsync(string key, string language, CancellationToken cancellationToken = default)
    {
        return _dbContext.LocalizedContents
            .FirstOrDefaultAsync(content => content.Key == key && content.Language == language, cancellationToken);
    }

    public async Task AddAsync(LocalizedContent content, CancellationToken cancellationToken = default)
    {
        await _dbContext.LocalizedContents.AddAsync(content, cancellationToken);
    }
}
