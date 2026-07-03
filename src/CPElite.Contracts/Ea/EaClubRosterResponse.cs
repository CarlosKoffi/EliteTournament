namespace CPElite.Contracts.Ea;

public sealed record EaClubRosterResponse(long EaClubId, string Platform, string? ClubName, IReadOnlyCollection<EaClubRosterPlayerResponse> Players);

