namespace CPElite.Contracts.Content;

public sealed record ContentCatalogResponse(
    string Language,
    IReadOnlyCollection<ContentItemResponse> Items);
