namespace CPElite.Contracts.Ea;

public sealed record EaProbeRequest(
    string Platform,
    string? ClubName,
    long? ClubId,
    string MatchType = "friendlyMatch",
    int MaxResults = 10);
