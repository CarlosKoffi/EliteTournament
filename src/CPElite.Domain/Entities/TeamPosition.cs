namespace CPElite.Domain.Entities;

public sealed class TeamPosition
{
    private TeamPosition() { }

    public TeamPosition(Guid id, Guid teamId, string name, string? description, int sortOrder, DateTimeOffset createdAt)
    {
        Id = id;
        TeamId = teamId;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Team? Team { get; private set; }
}
