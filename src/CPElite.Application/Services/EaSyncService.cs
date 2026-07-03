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
                GetInt(item, "proHeight") ?? GetInt(item, "height"),
                GetInt(item, "weight"),
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
                item.GetRawText(),
                syncedAt);
        }
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

        if (!document.RootElement.TryGetProperty("matches", out var matchGroups) ||
            matchGroups.ValueKind != JsonValueKind.Object ||
            !matchGroups.TryGetProperty("friendly", out var friendlies) ||
            friendlies.ValueKind != JsonValueKind.Array)
        {
            return new ParsedFriendlyMatches(matches, playerStats, clubStats);
        }

        foreach (var item in friendlies.EnumerateArray())
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

                    clubStats.Add(new EaMatchClubStat(
                        Guid.NewGuid(),
                        match.Id,
                        teamId,
                        aggregateClubId,
                        GetInt(clubAggregate.Value, "goals") ?? GetInt(clubAggregate.Value, "SCORE"),
                        GetInt(clubAggregate.Value, "assists"),
                        GetDouble(clubAggregate.Value, "rating"),
                        GetInt(clubAggregate.Value, "shots"),
                        GetInt(clubAggregate.Value, "passesmade"),
                        GetInt(clubAggregate.Value, "passattempts"),
                        GetInt(clubAggregate.Value, "tacklesmade"),
                        GetInt(clubAggregate.Value, "tackleattempts"),
                        GetInt(clubAggregate.Value, "saves"),
                        GetInt(clubAggregate.Value, "goalsconceded"),
                        GetInt(clubAggregate.Value, "redcards"),
                        GetInt(clubAggregate.Value, "mom"),
                        clubAggregate.Value.GetRawText()));
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
        return new EaMatchClubStatResponse(stat.EaClubId, stat.Goals, stat.Assists, stat.Rating, stat.Shots, stat.PassesMade, stat.PassAttempts, stat.PassSuccessRate, stat.TacklesMade, stat.TackleAttempts, stat.TackleSuccessRate, stat.Saves, stat.GoalsConceded, stat.RedCards, stat.PlayerOfTheMatch);
    }

    private static EaPlayerProfileSnapshotResponse ToPlayerProfileResponse(EaPlayerProfileSnapshot profile)
    {
        return new EaPlayerProfileSnapshotResponse(profile.EaPlayerId, profile.PlayerName, profile.ProName, profile.Position, profile.Matches, profile.WinRate, profile.Goals, profile.Assists, profile.AverageRating, profile.Height, profile.Weight, profile.Overall, profile.Shots, profile.ShotSuccessRate, profile.PassesMade, profile.PassAttempts, profile.PassSuccessRate, profile.TacklesMade, profile.TackleAttempts, profile.TackleSuccessRate, profile.Saves, profile.CleanSheets, profile.CleanSheetsGk, profile.PlayerOfTheMatch, profile.RedCards, profile.PrevGoals, profile.SyncedAt);
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
