using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class Team
{
    private readonly List<TeamMember> _members = [];
    private readonly List<TeamJoinRequest> _joinRequests = [];
    private readonly List<TeamPosition> _positions = [];
    private readonly List<TeamScheduleSlot> _scheduleSlots = [];
    private readonly List<TeamPlayerDemand> _playerDemands = [];

    private Team() { }

    public Team(Guid id, string name, string normalizedName, string? shortName, Platform platform, string? region, string inviteCode, Guid createdByUserId, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        NormalizedName = normalizedName;
        ShortName = shortName;
        Platform = platform;
        Region = region;
        InviteCode = inviteCode;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? ShortName { get; private set; }
    public Platform Platform { get; private set; }
    public string? Region { get; private set; }
    public string InviteCode { get; private set; } = string.Empty;
    public DateTimeOffset? InviteCodeExpiresAt { get; private set; }
    public long? EaClubId { get; private set; }
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? DiscordUrl { get; private set; }
    public string? TwitchUrl { get; private set; }
    public string? TikTokUrl { get; private set; }
    public string? TwitterUrl { get; private set; }
    public bool RequireJoinApproval { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<TeamMember> Members => _members;
    public IReadOnlyCollection<TeamJoinRequest> JoinRequests => _joinRequests;
    public IReadOnlyCollection<TeamPosition> Positions => _positions;
    public IReadOnlyCollection<TeamScheduleSlot> ScheduleSlots => _scheduleSlots;
    public IReadOnlyCollection<TeamPlayerDemand> PlayerDemands => _playerDemands;

    public TeamMember AddOwner(Guid userId, DateTimeOffset joinedAt)
    {
        var membership = TeamMember.Create(Id, userId, TeamRole.Owner, joinedAt);
        _members.Add(membership);
        return membership;
    }

    public void UpdateProfile(string name, string normalizedName, string? shortName, Platform platform, string? region, string? description, long? eaClubId, string? logoUrl, string? bannerUrl)
    {
        Name = name;
        NormalizedName = normalizedName;
        ShortName = shortName;
        Platform = platform;
        Region = region;
        Description = description;
        EaClubId = eaClubId;
        LogoUrl = logoUrl;
        BannerUrl = bannerUrl;
    }

    public void UpdateSocialLinks(string? discordUrl, string? twitchUrl, string? tikTokUrl, string? twitterUrl)
    {
        DiscordUrl = discordUrl;
        TwitchUrl = twitchUrl;
        TikTokUrl = tikTokUrl;
        TwitterUrl = twitterUrl;
    }

    public void UpdateSettings(bool requireJoinApproval)
    {
        RequireJoinApproval = requireJoinApproval;
    }

    public void ExpireInviteCode(DateTimeOffset expiredAt)
    {
        InviteCodeExpiresAt = expiredAt;
    }

    public void RegenerateInviteCode(string inviteCode, DateTimeOffset? expiresAt)
    {
        InviteCode = inviteCode;
        InviteCodeExpiresAt = expiresAt;
    }

    public bool IsInviteActive(DateTimeOffset now)
    {
        return !IsArchived && (InviteCodeExpiresAt is null || InviteCodeExpiresAt > now);
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        IsArchived = true;
        ArchivedAt = archivedAt;
    }
}
