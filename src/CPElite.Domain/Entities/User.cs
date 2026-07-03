using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class User
{
    private readonly List<TeamMember> _memberships = [];

    private User() { }

    public User(Guid id, string email, string normalizedEmail, string passwordHash, string displayName, string? gamertag, string? eaSportsId, string? discordUserId, Platform platform, string preferredLanguage, string timeZone, DateTimeOffset createdAt, string? profileImageUrl = null, long? eaClubId = null, string? eaClubName = null)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Gamertag = gamertag;
        EaSportsId = eaSportsId;
        DiscordUserId = discordUserId;
        Platform = platform;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
        ProfileImageUrl = profileImageUrl;
        EaClubId = eaClubId;
        EaClubName = eaClubName;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Gamertag { get; private set; }
    public string? EaSportsId { get; private set; }
    public string? DiscordUserId { get; private set; }
    public Platform Platform { get; private set; }
    public string PreferredLanguage { get; private set; } = "en";
    public string TimeZone { get; private set; } = "UTC";
    public string? ProfileImageUrl { get; private set; }
    public long? EaClubId { get; private set; }
    public string? EaClubName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public IReadOnlyCollection<TeamMember> Memberships => _memberships;

    public void MarkLoggedIn(DateTimeOffset loggedInAt)
    {
        LastLoginAt = loggedInAt;
    }

    public void UpdatePlayerProfile(string displayName, string? gamertag, string? eaSportsId, string? discordUserId, Platform platform, string preferredLanguage, string timeZone, string? profileImageUrl)
    {
        DisplayName = displayName;
        Gamertag = gamertag;
        EaSportsId = eaSportsId;
        DiscordUserId = discordUserId;
        Platform = platform;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
        ProfileImageUrl = profileImageUrl;
    }
}
