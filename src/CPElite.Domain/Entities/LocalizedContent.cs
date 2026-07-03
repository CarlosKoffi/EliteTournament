namespace CPElite.Domain.Entities;

public sealed class LocalizedContent
{
    private LocalizedContent() { }

    public LocalizedContent(Guid id, string key, string language, string value, string? section, string? description, DateTimeOffset updatedAt, Guid? updatedByUserId)
    {
        Id = id;
        Key = key;
        Language = language;
        Value = value;
        Section = section;
        Description = description;
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }

    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Language { get; private set; } = "fr";
    public string Value { get; private set; } = string.Empty;
    public string? Section { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public void Update(string value, string? section, string? description, DateTimeOffset updatedAt, Guid? updatedByUserId)
    {
        Value = value;
        Section = section;
        Description = description;
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }
}
