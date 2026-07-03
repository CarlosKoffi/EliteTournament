namespace CPElite.Contracts.Teams;

public sealed record TeamScheduleSlotResponse(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string? Label);
