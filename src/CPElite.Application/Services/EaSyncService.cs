using System.Text.Json;
using CPElite.Application.Abstractions;
using CPElite.Contracts.Ea;
using CPElite.Domain.Entities;

namespace CPElite.Application.Services;

public sealed class EaSyncService
{
    private static readonly string[] MatchTypes = ["friendlyMatch", "leagueMatch", "playoffMatch"];

    private readonly ITeamRepository _teams;
    private readonly IEaProClubsClient _client;
    private readonly IEaApiCacheRepository _cache;
    private readonly IEaSyncRepository _snapshots;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public EaSyncService(ITeamRepository teams, IEaProClubsClient client, IEaApiCacheRepository cache, IEaSyncRepository snapshots, IClock clock, IUnitOfWork unitOfWork)
    {
        _teams = teams;
        _client = client;
        _cache = cache;
        _snapshots = snapshots;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EaSyncResponse>> SyncTeamAsync(Guid teamId, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var team = await _teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null || team.IsArchived)
        {
            return Result<EaSyncResponse>.Failure(ErrorType.NotFound, "team.not_found", "Team was not found.");
        }

        if (team.EaClubId is null)
        {
            return Result<EaSyncResponse>.Failure(ErrorType.Validation, "team.ea_club_id_required", "Team is not linked to an EA club.");
        }

        var response = await SyncLinkedTeamAsync(team, forceRefresh, cancellationToken);
        return Result<EaSyncResponse>.Success(response);
    }

    public async Task<int> SyncAllLinkedTeamsAsync(CancellationToken cancellationToken = default)
    {
        var teams = await _teams.GetTeamsLinkedToEaAsync(cancellationToken);
        var synced = 0;

        foreach (var team in teams)
        {
            await SyncLinkedTeamAsync(team, forceRefresh: false, cancellationToken);
            synced++;
        }

        return synced;
    }

    public async Task<Result<EaClubSnapshotResponse>> GetClubSnapshotAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshots.GetClubSnapshotAsync(teamId, cancellationToken);
        if (snapshot is null)
        {
            return Result<EaClubSnapshotResponse>.Failure(ErrorType.NotFound, "ea.snapshot_not_found", "EA club data has not been synced yet.");
        }

        return Result<EaClubSnapshotResponse>.Success(new EaClubSnapshotResponse(snapshot.TeamId, snapshot.EaClubId, snapshot.Platform, snapshot.Name, snapshot.Abbreviation, snapshot.Division, snapshot.MembersCount, snapshot.SyncedAt));
    }

    public async Task<Result<EaMemberStatsSummaryResponse>> GetMemberStatsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshots.GetMemberStatsSnapshotAsync(teamId, cancellationToken);
        if (snapshot is null)
        {
            return Result<EaMemberStatsSummaryResponse>.Failure(ErrorType.NotFound, "ea.member_stats_not_found", "EA member stats have not been synced yet.");
        }

        var activeRosterKeys = ExtractActiveRosterKeys(snapshot.RawJson);
        var players = ParseMemberStats(snapshot.RawJson, activeRosterKeys).ToArray();
        return Result<EaMemberStatsSummaryResponse>.Success(new EaMemberStatsSummaryResponse(snapshot.TeamId, snapshot.EaClubId, snapshot.Platform, snapshot.SyncedAt, players.Length, players));
    }

    private async Task<EaSyncResponse> SyncLinkedTeamAsync(Team team, bool forceRefresh, CancellationToken cancellationToken)
    {
        var platform = ResolveEaPlatform(team);
        var eaClubId = team.EaClubId!.Value;
        var syncedAt = _clock.UtcNow;
        var clubInfoSynced = false;
        var memberStatsSynced = false;
        var matchTypesSynced = 0;

        var clubInfoJson = await GetCachedOrFetchAsync($"club-info:{platform}:{eaClubId}", $"/clubs/info", TimeSpan.FromHours(2), forceRefresh, () => _client.GetClubInfoJsonAsync(platform, eaClubId, cancellationToken), cancellationToken);
        if (clubInfoJson is not null)
        {
            var parsed = ParseClubInfo(clubInfoJson, eaClubId);
            await _snapshots.UpsertClubSnapshotAsync(new EaClubSnapshot(Guid.NewGuid(), team.Id, eaClubId, platform, parsed.Name, parsed.Abbreviation, parsed.Division, parsed.MembersCount, clubInfoJson, syncedAt), cancellationToken);
            clubInfoSynced = true;
        }

        var statsJson = await GetCachedOrFetchAsync($"member-stats:{platform}:{eaClubId}", $"/members/stats", TimeSpan.FromHours(2), forceRefresh, () => _client.GetMemberStatsJsonAsync(platform, eaClubId, cancellationToken), cancellationToken);
        if (statsJson is not null)
        {
            var activeRosterKeys = ExtractActiveRosterKeys(statsJson);
            await _snapshots.UpsertMemberStatsSnapshotAsync(new EaMemberStatsSnapshot(Guid.NewGuid(), team.Id, eaClubId, platform, statsJson, syncedAt), cancellationToken);
            await _snapshots.ReplacePlayerProfilesAsync(team.Id, ParsePlayerProfiles(statsJson, team.Id, eaClubId, platform, syncedAt, activeRosterKeys).ToArray(), cancellationToken);
            memberStatsSynced = true;
        }

        foreach (var matchType in MatchTypes)
        {
            var maxResults = matchType == "friendlyMatch" ? 100 : 50;
            var matchesJson = await GetCachedOrFetchAsync($"matches:{platform}:{eaClubId}:{matchType}", $"/clubs/matches", TimeSpan.FromMinutes(15), forceRefresh, () => _client.GetClubMatchesJsonAsync(platform, eaClubId, matchType, maxResults, cancellationToken), cancellationToken);
            if (matchesJson is null)
            {
                continue;
            }

            await _snapshots.UpsertMatchSnapshotAsync(new EaMatchSnapshot(Guid.NewGuid(), team.Id, eaClubId, platform, matchType, matchesJson, syncedAt), cancellationToken);
            if (matchType == "friendlyMatch")
            {
                var parsed = ParseFriendlyMatches(matchesJson, team.Id, eaClubId, platform, syncedAt);
                await _snapshots.UpsertFriendlyMatchesAsync(team.Id, parsed.Matches, parsed.PlayerStats, parsed.ClubStats, cancellationToken);
            }

            matchTypesSynced++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new EaSyncResponse(team.Id, eaClubId, platform, syncedAt, clubInfoSynced, memberStatsSynced, matchTypesSynced);
    }

    private async Task<string?> GetCachedOrFetchAsync(string cacheKey, string endpoint, TimeSpan lifetime, bool forceRefresh, Func<Task<string?>> fetch, CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync(cacheKey, cancellationToken);
        if (!forceRefresh && cached is not null && cached.IsFresh(_clock.UtcNow))
        {
            return cached.RawJson;
        }

        var rawJson = await fetch();
        if (rawJson is null)
        {
            return cached?.RawJson;
        }

        await _cache.UpsertAsync(new EaApiCacheEntry(Guid.NewGuid(), cacheKey, endpoint, rawJson, _clock.UtcNow, _clock.UtcNow.Add(lifetime)), cancellationToken);
        return rawJson;
    }

    private static string ResolveEaPlatform(Team team)
    {
        return team.Platform == Domain.Enums.Platform.Pc ? "pc" : "common-gen5";
    }

    private static ClubInfoFields ParseClubInfo(string rawJson, long eaClubId)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("clubs", out var clubs))
            {
                return ParseProClubsTrackerClubInfo(root);
            }

            JsonElement? club = null;
            if (clubs.ValueKind == JsonValueKind.Array && clubs.GetArrayLength() > 0)
            {
                club = clubs[0];
            }
            else if (clubs.ValueKind == JsonValueKind.Object && clubs.TryGetProperty(eaClubId.ToString(), out var keyedClub))
            {
                club = keyedClub;
            }

            if (club is null)
            {
                return new ClubInfoFields(null, null, null, null);
            }

            return new ClubInfoFields(
                GetString(club.Value, "name"),
                GetString(club.Value, "abbreviation"),
                GetInt(club.Value, "division"),
                GetInt(club.Value, "membersCount"));
        }
        catch (JsonException)
        {
            return new ClubInfoFields(null, null, null, null);
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
        {
            return value;
        }

        return property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var parsed) ? parsed : null;
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value))
        {
            return value;
        }

        return property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    public async Task<Result<IReadOnlyCollection<EaPlayerProfileSnapshotResponse>>> GetPlayerProfilesAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var profiles = await _snapshots.GetPlayerProfilesAsync(teamId, cancellationToken);
        return Result<IReadOnlyCollection<EaPlayerProfileSnapshotResponse>>.Success(profiles.Select(ToPlayerProfileResponse).ToArray());
    }

    public async Task<Result<IReadOnlyCollection<EaTournamentPlayerStatsResponse>>> GetTournamentPlayerStatsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var stats = await _snapshots.GetTournamentPlayerStatsAsync(teamId, cancellationToken);
        return Result<IReadOnlyCollection<EaTournamentPlayerStatsResponse>>.Success(stats.Select(stat => new EaTournamentPlayerStatsResponse(
            stat.EaPlayerId,
            stat.PlayerName,
            stat.Position,
            stat.Matches,
            stat.Goals,
            stat.Assists,
            stat.AverageRating,
            stat.PlayerOfTheMatch,
            stat.Shots,
            stat.PassesMade,
            stat.PassAttempts,
            stat.PassSuccessRate,
            stat.TacklesMade,
            stat.TackleAttempts,
            stat.TackleSuccessRate,
            stat.Saves,
            stat.GoalsConceded,
            stat.CleanSheetsAny,
            stat.CleanSheetsDef,
            stat.CleanSheetsGk,
            stat.RedCards,
            stat.SecondsPlayed)).ToArray());
    }

    public async Task<Result<IReadOnlyCollection<EaFriendlyMatchResponse>>> GetFriendlyMatchesAsync(Guid teamId, int take = 20, Guid? tournamentMatchId = null, CancellationToken cancellationToken = default)
    {
        var matches = await _snapshots.GetFriendlyMatchesAsync(teamId, Math.Clamp(take, 1, 50), cancellationToken, tournamentMatchId);
        return Result<IReadOnlyCollection<EaFriendlyMatchResponse>>.Success(matches.Select(ToFriendlyMatchResponse).ToArray());
    }

    public async Task<Result<EaFriendlyMatchDetailsResponse>> GetFriendlyMatchAsync(Guid teamId, string eaMatchId, CancellationToken cancellationToken = default)
    {
        var match = await _snapshots.GetFriendlyMatchAsync(teamId, eaMatchId, cancellationToken);
        if (match is null)
        {
            return Result<EaFriendlyMatchDetailsResponse>.Failure(ErrorType.NotFound, "ea.friendly_not_found", "Friendly match was not found in synced EA data.");
        }

        return Result<EaFriendlyMatchDetailsResponse>.Success(new EaFriendlyMatchDetailsResponse(
            ToFriendlyMatchResponse(match),
            match.ClubStats.OrderByDescending(stat => stat.EaClubId == match.EaClubId).Select(ToMatchClubStatResponse).ToArray(),
            match.PlayerStats.OrderByDescending(stat => stat.Goals ?? 0).ThenByDescending(stat => stat.Assists ?? 0).Select(ToMatchPlayerStatResponse).ToArray(),
            match.RawJson));
    }
    public async Task<Result<EaSyncResponse>> ImportFriendlyMatchesJsonByEaClubIdAsync(long eaClubId, string rawJson, string? platform = null, CancellationToken cancellationToken = default)
    {
        var team = await _teams.GetByEaClubIdAsync(eaClubId, cancellationToken);
        if (team is null)
        {
            return Result<EaSyncResponse>.Failure(ErrorType.NotFound, "team.ea_club_not_found", "No active team is linked to this EA club ID.");
        }

        return await ImportFriendlyMatchesJsonForTeamAsync(team, rawJson, platform, cancellationToken);
    }

    public async Task<Result<EaSyncResponse>> ImportFriendlyMatchesJsonAsync(Guid teamId, string rawJson, string? platform = null, CancellationToken cancellationToken = default)
    {
        var team = await _teams.GetByIdAsync(teamId, cancellationToken);
        if (team is null || team.IsArchived)
        {
            return Result<EaSyncResponse>.Failure(ErrorType.NotFound, "team.not_found", "Team was not found.");
        }

        return await ImportFriendlyMatchesJsonForTeamAsync(team, rawJson, platform, cancellationToken);
    }

    private async Task<Result<EaSyncResponse>> ImportFriendlyMatchesJsonForTeamAsync(Team team, string rawJson, string? platform, CancellationToken cancellationToken)
    {
        if (team.EaClubId is null)
        {
            return Result<EaSyncResponse>.Failure(ErrorType.Validation, "team.ea_club_id_required", "Team is not linked to an EA club.");
        }

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return Result<EaSyncResponse>.Failure(ErrorType.Validation, "ea.import_empty", "No match JSON was provided.");
        }

        var resolvedPlatform = string.IsNullOrWhiteSpace(platform) ? ResolveEaPlatform(team) : platform.Trim();
        var syncedAt = _clock.UtcNow;
        ParsedFriendlyMatches parsed;
        try
        {
            parsed = ParseFriendlyMatches(rawJson, team.Id, team.EaClubId.Value, resolvedPlatform, syncedAt);
        }
        catch (JsonException)
        {
            return Result<EaSyncResponse>.Failure(ErrorType.Validation, "ea.import_invalid_json", "The match JSON is not valid.");
        }

        if (parsed.Matches.Count == 0)
        {
            return Result<EaSyncResponse>.Failure(ErrorType.Validation, "ea.import_no_matches", "No friendly match with a valid matchId was found in the JSON.");
        }

        await _snapshots.UpsertMatchSnapshotAsync(new EaMatchSnapshot(Guid.NewGuid(), team.Id, team.EaClubId.Value, resolvedPlatform, "friendlyMatch", rawJson, syncedAt), cancellationToken);
        await _snapshots.UpsertFriendlyMatchesAsync(team.Id, parsed.Matches, parsed.PlayerStats, parsed.ClubStats, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<EaSyncResponse>.Success(new EaSyncResponse(team.Id, team.EaClubId.Value, resolvedPlatform, syncedAt, false, false, 1));
    }

    private static IEnumerable<EaPlayerStatsResponse> ParseMemberStats(string rawJson, IReadOnlySet<string> activeRosterKeys)
    {
        using var document = JsonDocument.Parse(rawJson);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in EnumerateObjects(document.RootElement))
        {
            var name = GetString(item, "name")
                ?? GetString(item, "playername")
                ?? GetString(item, "displayName")
                ?? GetString(item, "proName");

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var matches = GetInt(item, "gamesPlayed") ?? GetInt(item, "matches") ?? GetInt(item, "appearances");
            var goals = GetInt(item, "goals");
            var assists = GetInt(item, "assists");
            var rating = GetDouble(item, "ratingAve") ?? GetDouble(item, "averageRating") ?? GetDouble(item, "rating");

            if (matches is null && goals is null && assists is null && rating is null)
            {
                continue;
            }

            var eaPlayerId = GetString(item, "playerId") ?? GetString(item, "personaName") ?? GetString(item, "eaSportsId") ?? GetString(item, "gamertag") ?? name;
            if (!IsInActiveRoster(item, eaPlayerId, name, activeRosterKeys) || !seen.Add(eaPlayerId))
            {
                continue;
            }

            yield return new EaPlayerStatsResponse(
                name,
                GetString(item, "personaName") ?? GetString(item, "eaSportsId") ?? GetString(item, "gamertag"),
                matches,
                goals,
                assists,
                rating,
                GetString(item, "favoritePosition") ?? GetString(item, "position"));
        }
    }

    private static IEnumerable<EaPlayerProfileSnapshot> ParsePlayerProfiles(string rawJson, Guid teamId, long eaClubId, string platform, DateTimeOffset syncedAt, IReadOnlySet<string> activeRosterKeys)
    {
        using var document = JsonDocument.Parse(rawJson);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in EnumerateObjects(document.RootElement))
        {
            var name = GetString(item, "playername") ?? GetString(item, "name") ?? GetString(item, "displayName");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var matches = GetInt(item, "gamesPlayed") ?? GetInt(item, "matches") ?? GetInt(item, "appearances");
            var goals = GetInt(item, "goals");
            var assists = GetInt(item, "assists");
            var rating = GetDouble(item, "ratingAve") ?? GetDouble(item, "averageRating") ?? GetDouble(item, "rating");
            if (matches is null && goals is null && assists is null && rating is null)
            {
                continue;
            }

            if (!LooksLikeGeneralPlayerProfile(item, matches))
            {
                continue;
            }

            var eaPlayerId = GetString(item, "playerId") ?? GetString(item, "personaName") ?? GetString(item, "eaSportsId") ?? name;
            if (!IsInActiveRoster(item, eaPlayerId, name, activeRosterKeys) || !seen.Add(eaPlayerId))
            {
                continue;
            }

            yield return new EaPlayerProfileSnapshot(
                Guid.NewGuid(),
                teamId,
                eaClubId,
                platform,
                eaPlayerId,
                name,
                GetString(item, "proName"),
                GetString(item, "favoritePosition") ?? GetString(item, "position") ?? GetString(item, "pos"),
                matches,
                GetInt(item, "winRate"),
                goals,
                assists,
                rating,
                ReadHeightCm(item),
                ReadWeightKg(item),
                GetInt(item, "proOverall") ?? GetInt(item, "overall"),
                GetInt(item, "shots"),
                GetInt(item, "shotSuccessRate"),
                GetInt(item, "passesmade") ?? GetInt(item, "passesMade"),
                GetInt(item, "passattempts") ?? GetInt(item, "passAttempts"),
                GetDouble(item, "passSuccessRate"),
                GetInt(item, "tacklesmade") ?? GetInt(item, "tacklesMade"),
                GetInt(item, "tackleattempts") ?? GetInt(item, "tackleAttempts"),
                GetDouble(item, "tackleSuccessRate"),
                GetInt(item, "saves"),
                GetInt(item, "cleanSheetsDef") ?? GetInt(item, "cleansheetsdef") ?? GetInt(item, "cleanSheets"),
                GetInt(item, "cleanSheetsGK") ?? GetInt(item, "cleansheetsgk"),
                GetInt(item, "mom") ?? GetInt(item, "playerOfTheMatch") ?? GetInt(item, "manOfTheMatch"),
                GetInt(item, "redCards") ?? GetInt(item, "redcards"),
                GetInt(item, "prevGoals"),
                GetDoubleAny(item, "goalsPerMatch", "goalspermatch", "goalspg") ?? PerMatch(goals, matches),
                GetDoubleAny(item, "assistsPerMatch", "assistspermatch", "assistspg") ?? PerMatch(assists, matches),
                GetIntAny(item, "goalContributions", "goalsAssists", "goalsAndAssists") ?? SumNullable(goals, assists),
                GetDoubleAny(item, "goalContributionsPerMatch", "goalsAssistsPerMatch", "goalsAndAssistsPerMatch") ?? PerMatch(SumNullable(goals, assists), matches),
                GetDoubleAny(item, "passesMadePerMatch", "passesmadepermatch", "passesPerMatch") ?? PerMatch(GetInt(item, "passesmade") ?? GetInt(item, "passesMade"), matches),
                GetDoubleAny(item, "tacklesMadePerMatch", "tacklesmadepermatch", "tacklesPerMatch") ?? PerMatch(GetInt(item, "tacklesmade") ?? GetInt(item, "tacklesMade"), matches),
                GetDoubleAny(item, "playerOfTheMatchRate", "manOfTheMatchRate", "momRate") ?? Rate(GetInt(item, "mom") ?? GetInt(item, "playerOfTheMatch") ?? GetInt(item, "manOfTheMatch"), matches),
                GetDoubleAny(item, "cleanSheetsDefRate", "cleansheetsdefrate") ?? Rate(GetInt(item, "cleanSheetsDef") ?? GetInt(item, "cleansheetsdef") ?? GetInt(item, "cleanSheets"), matches),
                GetDoubleAny(item, "cleanSheetsGkRate", "cleansheetsgkrate") ?? Rate(GetInt(item, "cleanSheetsGK") ?? GetInt(item, "cleansheetsgk"), matches),
                GetIntAny(item, "allClubsMatches", "allclubsMatches", "allTimeMatches"),
                GetIntAny(item, "allClubsGoals", "allclubsGoals", "allTimeGoals"),
                GetIntAny(item, "allClubsAssists", "allclubsAssists", "allTimeAssists"),
                GetDoubleAny(item, "allClubsAverageRating", "allclubsAverageRating", "allTimeAverageRating"),
                GetIntAny(item, "allClubsPlayerOfTheMatch", "allclubsPlayerOfTheMatch", "allTimeManOfTheMatch"),
                GetDoubleAny(item, "allClubsPlayerOfTheMatchRate", "allclubsPlayerOfTheMatchRate", "allTimeManOfTheMatchRate"),
                GetIntAny(item, "allClubsGoalContributions", "allclubsGoalContributions", "allTimeGoalContributions") ?? SumNullable(GetIntAny(item, "allClubsGoals", "allclubsGoals", "allTimeGoals"), GetIntAny(item, "allClubsAssists", "allclubsAssists", "allTimeAssists")),
                GetDoubleAny(item, "allClubsGoalContributionsPerMatch", "allclubsGoalContributionsPerMatch", "allTimeGoalContributionsPerMatch") ?? PerMatch(SumNullable(GetIntAny(item, "allClubsGoals", "allclubsGoals", "allTimeGoals"), GetIntAny(item, "allClubsAssists", "allclubsAssists", "allTimeAssists")), GetIntAny(item, "allClubsMatches", "allclubsMatches", "allTimeMatches")),
                item.GetRawText(),
                syncedAt);
        }
    }

    private static bool LooksLikeGeneralPlayerProfile(JsonElement item, int? matches)
    {
        if (matches is > 0)
        {
            return true;
        }

        if (ReadHeightCm(item) is not null || ReadWeightKg(item) is not null || GetInt(item, "proOverall") is not null || GetInt(item, "overall") is not null)
        {
            return true;
        }

        if (GetIntAny(item, "allClubsMatches", "allclubsMatches", "allTimeMatches") is > 0)
        {
            return true;
        }

        if (HasAnyProperty(item,
                "goalsPerMatch",
                "assistsPerMatch",
                "goalContributionsPerMatch",
                "passesMadePerMatch",
                "tacklesMadePerMatch",
                "playerOfTheMatchRate",
                "cleanSheetsDefRate",
                "cleanSheetsGkRate",
                "winRate",
                "prevGoals"))
        {
            return true;
        }

        var passes = GetInt(item, "passesmade") ?? GetInt(item, "passesMade");
        var passAttempts = GetInt(item, "passattempts") ?? GetInt(item, "passAttempts");
        var shots = GetInt(item, "shots");
        var tackles = GetInt(item, "tacklesmade") ?? GetInt(item, "tacklesMade");
        var singleMatchSignals = HasAnyProperty(item,
            "secondsPlayed",
            "gameTime",
            "realtimegame",
            "realtimeidle",
            "goalsconceded",
            "vproattr",
            "archetypeid",
            "match_event_aggregate_0",
            "match_event_aggregate_1");

        if (singleMatchSignals)
        {
            return false;
        }

        return (passes is > 300 || passAttempts is > 300 || shots is > 80 || tackles is > 80);
    }

    private static IReadOnlySet<string> ExtractActiveRosterKeys(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in EnumerateActiveRosterObjects(document.RootElement))
        {
            AddPlayerIdentityKeys(item, keys);
        }

        return keys;
    }

    private static int? SumNullable(int? left, int? right) => left is null && right is null ? null : (left ?? 0) + (right ?? 0);

    private static double? PerMatch(int? value, int? matches) => matches > 0 && value is not null ? Math.Round((double)value.Value / matches.Value, 2) : null;

    private static double? Rate(int? value, int? matches) => matches > 0 && value is not null ? Math.Round((double)value.Value / matches.Value * 100, 2) : null;
    private static int? GetIntAny(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetInt(element, propertyName);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static double? GetDoubleAny(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetDouble(element, propertyName);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static bool? GetBoolAny(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
            {
                return number != 0;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var text = property.GetString();
                if (bool.TryParse(text, out var parsedBool))
                {
                    return parsedBool;
                }

                if (int.TryParse(text, out var parsedNumber))
                {
                    return parsedNumber != 0;
                }
            }
        }

        return null;
    }

    private static bool HasAnyProperty(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static int? ReadHeightCm(JsonElement item)
    {
        var value = GetInt(item, "proHeight")
            ?? GetInt(item, "height")
            ?? GetInt(item, "playerHeight")
            ?? GetInt(item, "avatarHeight")
            ?? GetInt(item, "virtualProHeight");

        if (value is null)
        {
            return null;
        }

        if (value is >= 55 and <= 90)
        {
            return (int)Math.Round(value.Value * 2.54);
        }

        return value is >= 120 and <= 230 ? value : null;
    }

    private static int? ReadWeightKg(JsonElement item)
    {
        var value = GetInt(item, "proWeight")
            ?? GetInt(item, "weight")
            ?? GetInt(item, "playerWeight")
            ?? GetInt(item, "avatarWeight")
            ?? GetInt(item, "virtualProWeight");

        if (value is null)
        {
            return null;
        }

        if (value is > 110 and <= 260)
        {
            return (int)Math.Round(value.Value * 0.45359237);
        }

        return value is >= 45 and <= 120 ? value : null;
    }

    private static IEnumerable<JsonElement> EnumerateActiveRosterObjects(JsonElement root)
    {
        if (root.TryGetProperty("memberStats", out var memberStats) &&
            memberStats.ValueKind == JsonValueKind.Object &&
            memberStats.TryGetProperty("members", out var members) &&
            members.ValueKind == JsonValueKind.Array)
        {
            return members.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object);
        }

        if (root.TryGetProperty("members", out var rootMembers) && rootMembers.ValueKind == JsonValueKind.Array)
        {
            return rootMembers.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object);
        }

        return EnumerateRosterArrays(root).SelectMany(array => array.EnumerateArray()).Where(item => item.ValueKind == JsonValueKind.Object);
    }

    private static IEnumerable<JsonElement> EnumerateRosterArrays(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array &&
                    IsRosterPropertyName(property.Name) &&
                    property.Value.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.Object))
                {
                    yield return property.Value;
                }

                foreach (var child in EnumerateRosterArrays(property.Value))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in EnumerateRosterArrays(item))
                {
                    yield return child;
                }
            }
        }
    }

    private static bool IsRosterPropertyName(string propertyName)
    {
        return propertyName.Equals("members", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("roster", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("currentMembers", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("clubMembers", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddPlayerIdentityKeys(JsonElement item, HashSet<string> keys)
    {
        AddKey(keys, GetString(item, "playerId"));
        AddKey(keys, GetString(item, "personaId"));
        AddKey(keys, GetString(item, "eaPlayerId"));
        AddKey(keys, GetString(item, "id"));
        AddKey(keys, GetString(item, "personaName"));
        AddKey(keys, GetString(item, "eaSportsId"));
        AddKey(keys, GetString(item, "gamertag"));
        AddKey(keys, GetString(item, "playername"));
        AddKey(keys, GetString(item, "name"));
        AddKey(keys, GetString(item, "displayName"));
        AddKey(keys, GetString(item, "proName"));
    }

    private static bool IsInActiveRoster(JsonElement item, string? eaPlayerId, string? playerName, IReadOnlySet<string> activeRosterKeys)
    {
        if (activeRosterKeys.Count == 0)
        {
            return true;
        }

        var candidates = new[]
        {
            eaPlayerId,
            playerName,
            GetString(item, "playerId"),
            GetString(item, "personaId"),
            GetString(item, "eaPlayerId"),
            GetString(item, "id"),
            GetString(item, "personaName"),
            GetString(item, "eaSportsId"),
            GetString(item, "gamertag"),
            GetString(item, "playername"),
            GetString(item, "name"),
            GetString(item, "displayName"),
            GetString(item, "proName")
        };

        return candidates.Any(candidate => IsKnownRosterKey(activeRosterKeys, candidate));
    }

    private static bool IsKnownRosterKey(IReadOnlySet<string> activeRosterKeys, string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && activeRosterKeys.Contains(value.Trim());
    }

    private static void AddKey(HashSet<string> keys, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            keys.Add(value.Trim());
        }
    }

    private static ParsedFriendlyMatches ParseFriendlyMatches(string rawJson, Guid teamId, long eaClubId, string platform, DateTimeOffset syncedAt)
    {
        using var document = JsonDocument.Parse(rawJson);
        var matches = new List<EaFriendlyMatch>();
        var playerStats = new List<EaMatchPlayerStat>();
        var clubStats = new List<EaMatchClubStat>();

        var friendlies = ResolveFriendlyMatchArray(document.RootElement);
        if (friendlies is null)
        {
            return new ParsedFriendlyMatches(matches, playerStats, clubStats);
        }

        foreach (var item in friendlies.Value.EnumerateArray())
        {
            var matchId = GetString(item, "matchId");
            var playedAt = GetUnixDate(item, "timestamp");
            if (string.IsNullOrWhiteSpace(matchId) || playedAt is null || !item.TryGetProperty("clubs", out var clubs) || clubs.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var clubsInMatch = clubs.EnumerateObject()
                .Select(property => ExtractClubParticipant(property.Name, property.Value))
                .Where(participant => participant is not null)
                .Cast<ClubParticipant>()
                .OrderByDescending(participant => participant.EaClubId == eaClubId)
                .ToArray();

            if (clubsInMatch.Length < 2)
            {
                continue;
            }

            var home = clubsInMatch[0];
            var away = clubsInMatch[1];
            var match = new EaFriendlyMatch(Guid.NewGuid(), teamId, eaClubId, platform, matchId, playedAt.Value, "friendlyMatch", home.EaClubId, home.Name, home.Score, away.EaClubId, away.Name, away.Score, item.GetRawText(), syncedAt);
            matches.Add(match);

            if (item.TryGetProperty("aggregate", out var aggregate) && aggregate.ValueKind == JsonValueKind.Object)
            {
                foreach (var clubAggregate in aggregate.EnumerateObject())
                {
                    if (!long.TryParse(clubAggregate.Name, out var aggregateClubId) || clubAggregate.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var clubEntry = TryGetObjectProperty(clubs, aggregateClubId.ToString());
                    clubStats.Add(BuildClubStat(match.Id, teamId, aggregateClubId, clubEntry, clubAggregate.Value));
                }
            }

            if (clubStats.All(stat => stat.EaFriendlyMatchId != match.Id))
            {
                foreach (var clubEntry in clubs.EnumerateObject())
                {
                    if (long.TryParse(clubEntry.Name, out var clubEntryId) && clubEntry.Value.ValueKind == JsonValueKind.Object)
                    {
                        clubStats.Add(BuildClubStat(match.Id, teamId, clubEntryId, clubEntry.Value, null));
                    }
                }
            }

            if (!item.TryGetProperty("players", out var players) || players.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var clubPlayers in players.EnumerateObject())
            {
                if (!long.TryParse(clubPlayers.Name, out var playerClubId) || clubPlayers.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var player in clubPlayers.Value.EnumerateObject())
                {
                    if (player.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var playerName = GetString(player.Value, "playername");
                    if (string.IsNullOrWhiteSpace(playerName))
                    {
                        continue;
                    }

                    playerStats.Add(new EaMatchPlayerStat(
                        Guid.NewGuid(),
                        match.Id,
                        teamId,
                        playerClubId,
                        player.Name,
                        playerName,
                        GetString(player.Value, "pos"),
                        GetInt(player.Value, "goals"),
                        GetInt(player.Value, "assists"),
                        GetDouble(player.Value, "rating"),
                        GetInt(player.Value, "mom") == 1,
                        GetInt(player.Value, "shots"),
                        GetInt(player.Value, "passesmade"),
                        GetInt(player.Value, "passattempts"),
                        GetInt(player.Value, "tacklesmade"),
                        GetInt(player.Value, "tackleattempts"),
                        GetInt(player.Value, "saves"),
                        GetInt(player.Value, "goalsconceded"),
                        GetInt(player.Value, "secondsPlayed"),
                        GetString(player.Value, "vproattr"),
                        GetInt(player.Value, "cleansheetsany"),
                        GetInt(player.Value, "cleansheetsdef"),
                        GetInt(player.Value, "cleansheetsgk"),
                        GetInt(player.Value, "ballDiveSaves"),
                        GetInt(player.Value, "crossSaves"),
                        GetInt(player.Value, "goodDirectionSaves"),
                        GetInt(player.Value, "parrySaves"),
                        GetInt(player.Value, "punchSaves"),
                        GetInt(player.Value, "reflexSaves"),
                        GetInt(player.Value, "redcards"),
                        GetInt(player.Value, "gameTime"),
                        GetInt(player.Value, "realtimegame"),
                        GetInt(player.Value, "realtimeidle"),
                        GetInt(player.Value, "archetypeid"),
                        GetString(player.Value, "match_event_aggregate_0"),
                        GetString(player.Value, "match_event_aggregate_1"),
                        GetString(player.Value, "match_event_aggregate_2"),
                        GetString(player.Value, "match_event_aggregate_3"),
                        player.Value.GetRawText()));
                }
            }
        }

        return new ParsedFriendlyMatches(matches, playerStats, clubStats);
    }

    private static JsonElement? ResolveFriendlyMatchArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("matches", out var matchGroups) &&
            matchGroups.ValueKind == JsonValueKind.Object &&
            matchGroups.TryGetProperty("friendly", out var friendlies) &&
            friendlies.ValueKind == JsonValueKind.Array)
        {
            return friendlies;
        }

        return null;
    }

    private static EaMatchClubStat BuildClubStat(Guid matchId, Guid teamId, long clubId, JsonElement? clubEntry, JsonElement? aggregate)
    {
        var details = clubEntry is { ValueKind: JsonValueKind.Object } entry && entry.TryGetProperty("details", out var rawDetails) && rawDetails.ValueKind == JsonValueKind.Object
            ? rawDetails
            : (JsonElement?)null;
        var kit = details is { ValueKind: JsonValueKind.Object } detailObject && detailObject.TryGetProperty("customKit", out var rawKit) && rawKit.ValueKind == JsonValueKind.Object
            ? rawKit
            : (JsonElement?)null;
        var rawJson = MergeClubRawJson(clubEntry, aggregate);

        return new EaMatchClubStat(
            Guid.NewGuid(),
            matchId,
            teamId,
            clubId,
            ReadInt(aggregate, "goals") ?? ReadInt(clubEntry, "goals") ?? ReadInt(aggregate, "SCORE") ?? ReadInt(clubEntry, "score"),
            ReadInt(aggregate, "assists"),
            ReadDouble(aggregate, "rating"),
            ReadInt(aggregate, "shots"),
            ReadInt(aggregate, "passesmade"),
            ReadInt(aggregate, "passattempts"),
            ReadInt(aggregate, "tacklesmade"),
            ReadInt(aggregate, "tackleattempts"),
            ReadInt(aggregate, "saves"),
            ReadInt(aggregate, "goalsconceded") ?? ReadInt(clubEntry, "goalsAgainst"),
            ReadInt(aggregate, "redcards"),
            ReadInt(aggregate, "mom"),
            ReadInt(clubEntry, "score"),
            ReadString(clubEntry, "result"),
            ReadInt(clubEntry, "wins"),
            ReadInt(clubEntry, "losses"),
            ReadInt(clubEntry, "ties"),
            ReadBool(clubEntry, "winnerByDnf"),
            ReadInt(details, "regionId"),
            ReadInt(details, "teamId") ?? ReadInt(clubEntry, "TEAM"),
            ReadString(kit, "stadName"),
            ReadString(kit, "crestAssetId"),
            ReadString(kit, "kitColor1"),
            ReadString(kit, "kitColor2"),
            ReadString(kit, "kitColor3"),
            ReadString(kit, "kitColor4"),
            rawJson);
    }

    private static string MergeClubRawJson(JsonElement? clubEntry, JsonElement? aggregate)
    {
        return $"{{\"club\":{(clubEntry is null ? "null" : clubEntry.Value.GetRawText())},\"aggregate\":{(aggregate is null ? "null" : aggregate.Value.GetRawText())}}}";
    }

    private static JsonElement? TryGetObjectProperty(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : null;
    }

    private static int? ReadInt(JsonElement? element, string propertyName) => element is { ValueKind: JsonValueKind.Object } value ? GetInt(value, propertyName) : null;

    private static double? ReadDouble(JsonElement? element, string propertyName) => element is { ValueKind: JsonValueKind.Object } value ? GetDouble(value, propertyName) : null;

    private static string? ReadString(JsonElement? element, string propertyName) => element is { ValueKind: JsonValueKind.Object } value ? GetString(value, propertyName) : null;

    private static bool? ReadBool(JsonElement? element, string propertyName)
    {
        if (element is not { ValueKind: JsonValueKind.Object } value || !value.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when property.TryGetInt32(out var number) => number != 0,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var boolean) => boolean,
            JsonValueKind.String when int.TryParse(property.GetString(), out var number) => number != 0,
            _ => null
        };
    }
    private static ClubParticipant? ExtractClubParticipant(string clubIdKey, JsonElement club)
    {
        if (!long.TryParse(clubIdKey, out var clubId))
        {
            return null;
        }

        var name = club.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Object ? GetString(details, "name") : null;
        return new ClubParticipant(clubId, name, GetInt(club, "score") ?? GetInt(club, "goals") ?? 0);
    }

    private static DateTimeOffset? GetUnixDate(JsonElement element, string propertyName)
    {
        var seconds = GetInt(element, propertyName);
        return seconds is null ? null : DateTimeOffset.FromUnixTimeSeconds(seconds.Value);
    }

    private static EaFriendlyMatchResponse ToFriendlyMatchResponse(EaFriendlyMatch match)
    {
        return new EaFriendlyMatchResponse(match.EaMatchId, match.PlayedAt, match.MatchType, match.HomeEaClubId, match.HomeClubName, match.HomeScore, match.AwayEaClubId, match.AwayClubName, match.AwayScore, match.PlayerStats.Count);
    }

    private static EaMatchPlayerStatResponse ToMatchPlayerStatResponse(EaMatchPlayerStat stat)
    {
        return new EaMatchPlayerStatResponse(stat.EaClubId, stat.EaPlayerId, stat.PlayerName, stat.Position, stat.Goals, stat.Assists, stat.Rating, stat.PlayerOfTheMatch, stat.Shots, stat.PassesMade, stat.PassAttempts, stat.PassSuccessRate, stat.TacklesMade, stat.TackleAttempts, stat.TackleSuccessRate, stat.Saves, stat.GoalsConceded, stat.SecondsPlayed, stat.VproAttributes, stat.CleanSheetsAny, stat.CleanSheetsDef, stat.CleanSheetsGk, stat.BallDiveSaves, stat.CrossSaves, stat.GoodDirectionSaves, stat.ParrySaves, stat.PunchSaves, stat.ReflexSaves, stat.RedCards, stat.GameTime, stat.RealtimeGame, stat.RealtimeIdle, stat.ArchetypeId, stat.MatchEventAggregate0, stat.MatchEventAggregate1, stat.MatchEventAggregate2, stat.MatchEventAggregate3);
    }

    private static EaMatchClubStatResponse ToMatchClubStatResponse(EaMatchClubStat stat)
    {
        return new EaMatchClubStatResponse(stat.EaClubId, stat.Goals, stat.Assists, stat.Rating, stat.Shots, stat.PassesMade, stat.PassAttempts, stat.PassSuccessRate, stat.TacklesMade, stat.TackleAttempts, stat.TackleSuccessRate, stat.Saves, stat.GoalsConceded, stat.RedCards, stat.PlayerOfTheMatch, stat.Score, stat.Result, stat.Wins, stat.Losses, stat.Ties, stat.WinnerByDnf, stat.RegionId, stat.EaTeamId, stat.StadiumName, stat.CrestAssetId, stat.KitColor1, stat.KitColor2, stat.KitColor3, stat.KitColor4);
    }

    private static EaPlayerProfileSnapshotResponse ToPlayerProfileResponse(EaPlayerProfileSnapshot profile)
    {
        return new EaPlayerProfileSnapshotResponse(profile.EaPlayerId, profile.PlayerName, profile.ProName, profile.Position, profile.Matches, profile.WinRate, profile.Goals, profile.Assists, profile.AverageRating, profile.Height, profile.Weight, profile.Overall, profile.Shots, profile.ShotSuccessRate, profile.PassesMade, profile.PassAttempts, profile.PassSuccessRate, profile.TacklesMade, profile.TackleAttempts, profile.TackleSuccessRate, profile.Saves, profile.CleanSheets, profile.CleanSheetsGk, profile.PlayerOfTheMatch, profile.RedCards, profile.PrevGoals, profile.GoalsPerMatch, profile.AssistsPerMatch, profile.GoalContributions, profile.GoalContributionsPerMatch, profile.PassesMadePerMatch, profile.TacklesMadePerMatch, profile.PlayerOfTheMatchRate, profile.CleanSheetsDefRate, profile.CleanSheetsGkRate, profile.AllClubsMatches, profile.AllClubsGoals, profile.AllClubsAssists, profile.AllClubsAverageRating, profile.AllClubsPlayerOfTheMatch, profile.AllClubsPlayerOfTheMatchRate, profile.AllClubsGoalContributions, profile.AllClubsGoalContributionsPerMatch, profile.SyncedAt, ReadIsManager(profile.RawJson));
    }

    private static bool ReadIsManager(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            return ReadIsManager(document.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ReadIsManager(JsonElement item)
    {
        if (GetBoolAny(item, "isManager", "manager", "isCaptain", "captain", "isAdmin", "clubAdmin", "isOwner", "owner") is { } flag)
        {
            return flag;
        }

        var role = GetString(item, "role") ?? GetString(item, "clubRole") ?? GetString(item, "memberRole") ?? GetString(item, "memberType") ?? GetString(item, "accessLevel") ?? GetString(item, "title");
        return !string.IsNullOrWhiteSpace(role) &&
            (role.Contains("manager", StringComparison.OrdinalIgnoreCase)
             || role.Contains("owner", StringComparison.OrdinalIgnoreCase)
             || role.Contains("admin", StringComparison.OrdinalIgnoreCase)
             || role.Contains("captain", StringComparison.OrdinalIgnoreCase)
             || role.Equals("gm", StringComparison.OrdinalIgnoreCase)
             || role.Equals("co-gm", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
            {
                foreach (var child in EnumerateObjects(property.Value))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in EnumerateObjects(item))
                {
                    yield return child;
                }
            }
        }
    }

    private sealed record ClubInfoFields(string? Name, string? Abbreviation, int? Division, int? MembersCount);
    private sealed record ClubParticipant(long EaClubId, string? Name, int Score);
    private sealed record ParsedFriendlyMatches(IReadOnlyCollection<EaFriendlyMatch> Matches, IReadOnlyCollection<EaMatchPlayerStat> PlayerStats, IReadOnlyCollection<EaMatchClubStat> ClubStats);

    private static ClubInfoFields ParseProClubsTrackerClubInfo(JsonElement root)
    {
        var name = root.TryGetProperty("clubInfoData", out var clubInfoData) && clubInfoData.ValueKind == JsonValueKind.Object
            ? clubInfoData.EnumerateObject().Select(property => GetString(property.Value, "name")).FirstOrDefault(value => value is not null)
            : null;

        var division = root.TryGetProperty("overallStats", out var overallStats)
            ? GetInt(overallStats, "currentDivision") ?? GetInt(overallStats, "bestDivision")
            : null;

        int? membersCount = root.TryGetProperty("memberStats", out var memberStats)
            && memberStats.TryGetProperty("members", out var members)
            && members.ValueKind == JsonValueKind.Array
                ? members.GetArrayLength()
                : null;

        return new ClubInfoFields(name, null, division, membersCount);
    }
}
