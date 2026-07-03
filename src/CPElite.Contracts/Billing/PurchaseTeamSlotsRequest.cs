using CPElite.Contracts.Common;

namespace CPElite.Contracts.Billing;

public sealed record PurchaseTeamSlotsRequest(TeamSlotPackageSize PackageSize, AccessProvider Provider, string ProviderTransactionId);
