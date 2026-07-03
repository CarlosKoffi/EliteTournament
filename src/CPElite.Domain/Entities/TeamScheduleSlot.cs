namespace CPElite.Domain.Entities;

public sealed class TeamScheduleSlot
{
    private TeamScheduleSlot() { }

    public TeamScheduleSlot(Guid id, Guid teamId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, string? label, DateTimeOffset createdAt)
    {
        Id = id;
        TeamId = teamId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        Label = label;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string? Label { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Team? Team { get; private set; }
}
