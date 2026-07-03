namespace CPElite.Contracts.Ea;

public sealed record EaProbeResponse(
    DateTimeOffset TestedAt,
    string Platform,
    string? ClubName,
    long? ClubId,
    IReadOnlyCollection<EaProbeStepResponse> Steps,
    EaProbeExtractedDataResponse? Extracted = null);
