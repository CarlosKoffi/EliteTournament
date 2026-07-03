using CPElite.Application.Abstractions;
using CPElite.Contracts.Content;
using CPElite.Domain.Entities;

namespace CPElite.Application.Services;

public sealed class ContentService
{
    private readonly IContentRepository _content;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public ContentService(IContentRepository content, IClock clock, IUnitOfWork unitOfWork)
    {
        _content = content;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ContentCatalogResponse>> GetCatalogAsync(string? language, CancellationToken cancellationToken = default)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var items = await _content.GetByLanguageAsync(normalizedLanguage, cancellationToken);
        return Result<ContentCatalogResponse>.Success(new ContentCatalogResponse(normalizedLanguage, items.Select(ToResponse).ToArray()));
    }

    public async Task<Result<ContentItemResponse>> UpsertAsync(Guid updatedByUserId, UpsertContentItemRequest request, CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(request.Key);
        var language = NormalizeLanguage(request.Language);

        if (string.IsNullOrWhiteSpace(key))
        {
            return Result<ContentItemResponse>.Failure(ErrorType.Validation, "content.key_required", "Content key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            return Result<ContentItemResponse>.Failure(ErrorType.Validation, "content.value_required", "Content value is required.");
        }

        var existing = await _content.GetAsync(key, language, cancellationToken);
        if (existing is null)
        {
            existing = new LocalizedContent(
                Guid.NewGuid(),
                key,
                language,
                request.Value.Trim(),
                NormalizeOptional(request.Section),
                NormalizeOptional(request.Description),
                _clock.UtcNow,
                updatedByUserId);

            await _content.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.Update(
                request.Value.Trim(),
                NormalizeOptional(request.Section),
                NormalizeOptional(request.Description),
                _clock.UtcNow,
                updatedByUserId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ContentItemResponse>.Success(ToResponse(existing));
    }

    private static ContentItemResponse ToResponse(LocalizedContent content)
    {
        return new ContentItemResponse(content.Key, content.Language, content.Value, content.Section, content.Description, content.UpdatedAt);
    }

    private static string NormalizeLanguage(string? language)
    {
        var normalized = string.IsNullOrWhiteSpace(language) ? "fr" : language.Trim().ToLowerInvariant();
        return normalized.Length > 10 ? normalized[..10] : normalized;
    }

    private static string NormalizeKey(string key)
    {
        return key.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
