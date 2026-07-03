namespace CPElite.Contracts.Content;

public sealed record ContentItemResponse(
    string Key,
    string Language,
    string Value,
    string? Section,
    string? Description,
    DateTimeOffset UpdatedAt);
