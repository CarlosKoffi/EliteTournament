namespace CPElite.Contracts.Ea;

public sealed record EaClubSearchResponse(string ClubName, string Platform, IReadOnlyCollection<EaClubSearchResultResponse> Results);
