namespace CPElite.Contracts.Billing;

public sealed record TeamAccessSummaryResponse(Guid TeamId, string TeamName, int TotalSlots, int UsedSlots, int FreeSlots, decimal PricePaid, IReadOnlyCollection<TeamSlotHolderResponse> Holders);
