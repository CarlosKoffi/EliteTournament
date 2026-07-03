namespace CPElite.Domain.Entities;

public sealed class EaDiagnosticProbe
{
    private EaDiagnosticProbe() { }

    public EaDiagnosticProbe(Guid id, string stepName, string endpoint, string platform, string? clubName, long? clubId, bool success, int? statusCode, string? error, string? rawPreview, int? rawLength, DateTimeOffset createdAt)
    {
        Id = id;
        StepName = stepName;
        Endpoint = endpoint;
        Platform = platform;
        ClubName = clubName;
        ClubId = clubId;
        Success = success;
        StatusCode = statusCode;
        Error = error;
        RawPreview = rawPreview;
        RawLength = rawLength;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string StepName { get; private set; } = string.Empty;
    public string Endpoint { get; private set; } = string.Empty;
    public string Platform { get; private set; } = string.Empty;
    public string? ClubName { get; private set; }
    public long? ClubId { get; private set; }
    public bool Success { get; private set; }
    public int? StatusCode { get; private set; }
    public string? Error { get; private set; }
    public string? RawPreview { get; private set; }
    public int? RawLength { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
