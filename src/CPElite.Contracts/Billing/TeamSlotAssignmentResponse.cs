namespace CPElite.Contracts.Billing;

public sealed record TeamSlotAssignmentResponse(Guid? AssignmentId, Guid TeamId, Guid UserId, bool Assigned, string Message);
