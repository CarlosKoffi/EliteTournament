using CPElite.Contracts.Common;

namespace CPElite.Contracts.Billing;

public sealed record PurchaseIndividualAccessRequest(AccessProvider Provider, string ProviderTransactionId);
