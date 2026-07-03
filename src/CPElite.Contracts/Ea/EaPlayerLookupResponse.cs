namespace CPElite.Contracts.Ea;

public sealed record EaPlayerLookupResponse(
    string EaSportsId,
    string Platform,
    bool Found,
    string Source,
    IReadOnlyDictionary<string, string?> Fields,
    string? RawJson,
    long? ClubId = null);
