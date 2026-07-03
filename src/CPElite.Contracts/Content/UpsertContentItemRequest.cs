namespace CPElite.Contracts.Content;

public sealed record UpsertContentItemRequest(
    string Key,
    string Language,
    string Value,
    string? Section = null,
    string? Description = null);
