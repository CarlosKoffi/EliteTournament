using CPElite.Domain.Enums;

namespace CPElite.Domain.Entities;

public sealed class TournamentMoment
{
    private TournamentMoment() { }

    public TournamentMoment(Guid id, Guid tournamentId, Guid? matchId, Guid? teamId, Guid? userId, string? discordUserId, TournamentMomentType type, string title, string message, string? payloadJson, DateTimeOffset createdAt)
    {
        Id = id;
        TournamentId = tournamentId;
        MatchId = matchId;
        TeamId = teamId;
        UserId = userId;
        DiscordUserId = discordUserId;
        Type = type;
        Title = title;
        Message = message;
        PayloadJson = payloadJson;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid? MatchId { get; private set; }
    public Guid? TeamId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? DiscordUserId { get; private set; }
    public TournamentMomentType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? PayloadJson { get; private set; }
    public bool IsPublishedToDiscord { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedToDiscordAt { get; private set; }

    public void MarkPublishedToDiscord(DateTimeOffset publishedAt)
    {
        IsPublishedToDiscord = true;
        PublishedToDiscordAt = publishedAt;
    }
}
