using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class UserTournamentAccess
{
    private UserTournamentAccess() { }

    public UserTournamentAccess(Guid id, Guid userId, AccessProvider provider, string providerTransactionId, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Provider = provider;
        ProviderTransactionId = providerTransactionId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public AccessProvider Provider { get; private set; }
    public string ProviderTransactionId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public User? User { get; private set; }
}
