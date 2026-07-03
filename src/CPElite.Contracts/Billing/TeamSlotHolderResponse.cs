namespace CPElite.Contracts.Billing;

public sealed record TeamSlotHolderResponse(Guid UserId, string DisplayName, string? Gamertag, DateTimeOffset AssignedAt);
