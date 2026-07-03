using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TeamSlotPackage
{
    private TeamSlotPackage() { }

    public TeamSlotPackage(Guid id, Guid teamId, Guid purchasedByUserId, int slotCount, decimal pricePaid, string currency, AccessProvider provider, string providerTransactionId, DateTimeOffset createdAt)
    {
        Id = id;
        TeamId = teamId;
        PurchasedByUserId = purchasedByUserId;
        SlotCount = slotCount;
        PricePaid = pricePaid;
        Currency = currency;
        Provider = provider;
        ProviderTransactionId = providerTransactionId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid PurchasedByUserId { get; private set; }
    public int SlotCount { get; private set; }
    public decimal PricePaid { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public AccessProvider Provider { get; private set; }
    public string ProviderTransactionId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public Team? Team { get; private set; }
    public User? PurchasedByUser { get; private set; }
}
