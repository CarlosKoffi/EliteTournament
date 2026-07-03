namespace CPElite.Contracts.Ea;

public sealed record EaProbeStepResponse(
    string Name,
    string Endpoint,
    bool Success,
    int? StatusCode,
    string? Error,
    int? RawLength,
    string? RawPreview);
