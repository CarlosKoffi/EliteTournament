namespace CPElite.Contracts.Ea;

public sealed record EaClubSearchResultResponse(
    long EaClubId,
    string Name,
    string? Abbreviation,
    string Platform,
    int? Division,
    int? MembersCount,
    string Source,
    bool IsInApplication = false,
    Guid? ApplicationTeamId = null,
    string? ApplicationTeamName = null);
