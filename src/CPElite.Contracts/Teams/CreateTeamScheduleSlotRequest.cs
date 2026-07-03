namespace CPElite.Contracts.Teams;

public sealed record CreateTeamScheduleSlotRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string? Label);
