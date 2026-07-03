using System.Text.Json;
using CPElite.Application.Abstractions;
using CPElite.Contracts.Ea;
using CPElite.Domain.Entities;

namespace CPElite.Application.Services;

public sealed class EaDiagnosticsService
{
    private readonly IEaDiagnosticsClient _client;
    private readonly IEaDiagnosticsRepository _repository;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public EaDiagnosticsService(IEaDiagnosticsClient client, IEaDiagnosticsRepository repository, IClock clock, IUnitOfWork unitOfWork)
    {
        _client = client;
        _repository = repository;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EaProbeResponse>> ProbeAsync(EaProbeRequest request, CancellationToken cancellationToken = default)
    {
        var platform = string.IsNullOrWhiteSpace(request.Platform) ? "common-gen5" : request.Platform.Trim();
        var matchType = string.IsNullOrWhiteSpace(request.MatchType) ? "friendlyMatch" : request.MatchType.Trim();
        var maxResults = Math.Clamp(request.MaxResults, 1, 50);
        var testedAt = _clock.UtcNow;
        var results = new List<EaProbeStepResponse>();
        var rawPayloads = new List<ProbeRawPayload>();
        var resolvedClubId = request.ClubId;

        if (!string.IsNullOrWhiteSpace(request.ClubName))
        {
            var search = await ProbeStepAsync("proclubstracker-search", $"https://proclubstracker.com/api/clubs/search?clubName={Uri.EscapeDataString(request.ClubName.Trim())}&platform={Uri.EscapeDataString(platform)}", platform, request.ClubName, resolvedClubId, testedAt, results, rawPayloads, cancellationToken);
            resolvedClubId ??= TryExtractClubId(search.RawPreview);

            await ProbeStepAsync("leaderboard-club-search", $"/allTimeLeaderboard/search?platform={Uri.EscapeDataString(platform)}&clubName={Uri.EscapeDataString(request.ClubName.Trim())}", platform, request.ClubName, resolvedClubId, testedAt, results, rawPayloads, cancellationToken);
            await ProbeStepAsync("legacy-club-search", $"/clubs/search?platform={Uri.EscapeDataString(platform)}&clubName={Uri.EscapeDataString(request.ClubName.Trim())}", platform, request.ClubName, resolvedClubId, testedAt, results, rawPayloads, cancellationToken);
        }

        if (resolvedClubId is not null)
        {
            await ProbeStepAsync("proclubstracker-club-details", $"https://proclubstracker.com/api/clubs/{resolvedClubId.Value}?platform={Uri.EscapeDataString(platform)}", platform, request.ClubName, resolvedClubId, testedAt, results, rawPayloads, cancellationToken);
            await ProbeStepAsync("club-info", $"/clubs/info?platform={Uri.EscapeDataString(platform)}&clubIds={resolvedClubId.Value}", platform, request.ClubName, resolvedClubId, testedAt, results, rawPayloads, cancellationToken);
            await ProbeStepAsync("club-matches", $"/clubs/matches?matchType={Uri.EscapeDataString(matchType)}&platform={Uri.EscapeDataString(platform)}&clubIds={resolvedClubId.Value}&maxResultCount={maxResults}", platform, request.ClubName, resolvedClubId, testedAt, results, rawPayloads, cancellationToken);
            await ProbeStepAsync("member-stats", $"/members/stats?platform={Uri.EscapeDataString(platform)}&clubId={resolvedClubId.Value}", platform, request.ClubName, resolvedClubId, testedAt, results, rawPayloads, cancellationToken);
            await ProbeStepAsync("member-career-stats", $"/members/career/stats?platform={Uri.EscapeDataString(platform)}&clubId={resolvedClubId.Value}", platform, request.ClubName, resolvedClubId, testedAt, results, rawPayloads, cancellationToken);
        }

        if (results.Count == 0)
        {
            return Result<EaProbeResponse>.Failure(ErrorType.Validation, "ea.probe_input_required", "Provide a club name, a club ID, or both.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var extracted = ExtractProbeData(rawPayloads, resolvedClubId, matchType);
        return Result<EaProbeResponse>.Success(new EaProbeResponse(testedAt, platform, string.IsNullOrWhiteSpace(request.ClubName) ? null : request.ClubName.Trim(), resolvedClubId, results, extracted));
    }

    private async Task<EaProbeStepResponse> ProbeStepAsync(string name, string endpoint, string platform, string? clubName, long? clubId, DateTimeOffset testedAt, List<EaProbeStepResponse> results, List<ProbeRawPayload> rawPayloads, CancellationToken cancellationToken)
    {
        var result = await _client.ProbeAsync(endpoint, cancellationToken);
        var preview = CreatePreview(result.RawBody);
        var response = new EaProbeStepResponse(name, endpoint, result.Success, result.StatusCode, result.Error, result.RawBody?.Length, preview);
        results.Add(response);
        if (result.Success && !string.IsNullOrWhiteSpace(result.RawBody))
        {
            rawPayloads.Add(new ProbeRawPayload(name, endpoint, result.RawBody));
        }

        await _repository.AddAsync(new EaDiagnosticProbe(
            Guid.NewGuid(),
            name,
            endpoint,
            platform,
            string.IsNullOrWhiteSpace(clubName) ? null : clubName.Trim(),
            clubId,
            result.Success,
            result.StatusCode,
            result.Error,
            preview,
            result.RawBody?.Length,
            testedAt), cancellationToken);

        return response;
    }

    private static EaProbeExtractedDataResponse ExtractProbeData(IReadOnlyCollection<ProbeRawPayload> payloads, long? clubId, string matchType)
    {
        EaProbeClubDataResponse? club = null;
        var players = new Dictionary<string, EaProbePlayerDataResponse>(StringComparer.OrdinalIgnoreCase);
        var matches = new Dictionary<string, EaProbeMatchDataResponse>(StringComparer.OrdinalIgnoreCase);
        var rawFields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var payload in payloads)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload.RawJson);
                club ??= TryExtractClubData(doc.RootElement, clubId);

                foreach (var player in ExtractPlayers(doc.RootElement))
                {
                    var key = string.IsNullOrWhiteSpace(player.EaSportsId) ? player.Name : player.EaSportsId;
                    players.TryAdd(key, player);
                }

                foreach (var match in ExtractMatches(doc.RootElement, matchType, clubId))
                {
                    var key = match.MatchId ?? $"{match.PlayedAt:O}:{match.HomeClubName}:{match.AwayClubName}:{match.HomeScore}:{match.AwayScore}";
                    matches.TryAdd(key, match);
                }

                AddInterestingFields(rawFields, payload.Name, doc.RootElement);
            }
            catch (JsonException)
            {
                rawFields.TryAdd($"{payload.Name}.parseError", "JSON non parsable");
            }
        }

        return new EaProbeExtractedDataResponse(
            club,
            players.Values.Take(80).ToArray(),
            matches.Values.Take(50).ToArray(),
            rawFields.Take(60).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase));
    }

    private static EaProbeClubDataResponse? TryExtractClubData(JsonElement root, long? requestedClubId)
    {
        foreach (var obj in EnumerateObjects(root))
        {
            var clubId = FindLong(obj, "clubId", "id") ?? requestedClubId;
            var name = FindString(obj, "clubName", "name", "teamName");
            var hasClubShape = clubId is not null || name is not null && HasAnyProperty(obj, "division", "currentDivision", "wins", "losses", "membersCount", "memberCount", "skillRating");

            if (!hasClubShape)
            {
                continue;
            }

            return new EaProbeClubDataResponse(
                clubId,
                name,
                FindString(obj, "abbr", "abbreviation", "shortName", "clubAbbr"),
                FindInt(obj, "division", "currentDivision", "bestDivision"),
                FindInt(obj, "membersCount", "memberCount", "members", "memberTotal"),
                FindInt(obj, "wins", "totalWins"),
                FindInt(obj, "losses", "totalLosses"),
                FindInt(obj, "draws", "ties", "totalDraws"),
                FindInt(obj, "gamesPlayed", "matches", "played"),
                FindInt(obj, "goalsFor", "goals", "goalsScored"),
                FindInt(obj, "goalsAgainst", "goalsConceded"),
                FindInt(obj, "skillRating", "rating", "rankingPoints"));
        }

        return null;
    }

    private static IEnumerable<EaProbePlayerDataResponse> ExtractPlayers(JsonElement root)
    {
        foreach (var obj in EnumerateObjects(root))
        {
            var name = FindString(obj, "playername", "displayName", "proName", "personaName", "name", "playerName");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var hasMetric = HasAnyProperty(obj, "goals", "assists", "rating", "ratingAve", "averageRating", "gamesPlayed", "matches", "favoritePosition", "position", "passesMade", "tacklesMade", "motm", "mom");
            if (!hasMetric)
            {
                continue;
            }

            yield return new EaProbePlayerDataResponse(
                name,
                FindString(obj, "eaSportsId", "gamertag", "personaName", "nucleusId", "playerId", "pid"),
                FindString(obj, "proName"),
                FindString(obj, "favoritePosition", "position", "pos"),
                FindInt(obj, "gamesPlayed", "matches", "appearances", "played"),
                FindInt(obj, "winRate"),
                FindInt(obj, "goals"),
                FindInt(obj, "assists"),
                FindDouble(obj, "ratingAve", "averageRating", "rating", "overall"),
                FindInt(obj, "proHeight", "height"),
                FindInt(obj, "proOverall", "overall"),
                FindInt(obj, "shotSuccessRate"),
                FindInt(obj, "passesMade", "passesCompleted", "passSuccess", "passes"),
                FindInt(obj, "passesAttempted", "passAttempts"),
                FindDouble(obj, "passAccuracy", "passAccuracyPercentage", "passSuccessRate"),
                FindInt(obj, "tacklesMade", "tacklesWon", "tackles"),
                FindInt(obj, "tacklesAttempted", "tackleAttempts"),
                FindDouble(obj, "tackleSuccess", "tackleSuccessRate"),
                FindInt(obj, "cleanSheetsDef"),
                FindInt(obj, "cleanSheetsGK", "cleanSheetsGk"),
                FindInt(obj, "motm", "mom", "playerOfTheMatch", "manOfTheMatch"),
                FindInt(obj, "redCards", "redcards"),
                FindInt(obj, "prevGoals"));
        }
    }

    private static IEnumerable<EaProbeMatchDataResponse> ExtractMatches(JsonElement root, string matchType, long? requestedClubId)
    {
        var trackerMatches = ExtractTrackerMatchesFromRoot(root, matchType, requestedClubId).ToArray();
        if (trackerMatches.Length > 0)
        {
            foreach (var match in trackerMatches)
            {
                yield return match;
            }

            yield break;
        }

        foreach (var obj in EnumerateObjects(root))
        {
            var trackerMatch = TryExtractTrackerMatch(obj, matchType, requestedClubId);
            if (trackerMatch is not null)
            {
                yield return trackerMatch;
                continue;
            }

            var matchId = FindString(obj, "matchId", "id", "gameId");
            var homeScore = FindInt(obj, "homeScore", "club1Score", "score1", "homeGoals");
            var awayScore = FindInt(obj, "awayScore", "club2Score", "score2", "awayGoals");
            var homeName = FindString(obj, "homeClubName", "club1Name", "homeName", "team1Name");
            var awayName = FindString(obj, "awayClubName", "club2Name", "awayName", "team2Name");

            var hasMatchShape = matchId is not null || homeScore is not null && awayScore is not null || homeName is not null && awayName is not null;
            if (!hasMatchShape)
            {
                continue;
            }

            yield return new EaProbeMatchDataResponse(
                matchId,
                FindDate(obj, "timestamp", "date", "playedAt", "createdAt", "matchDate"),
                FindString(obj, "matchType", "type") ?? matchType,
                homeName,
                awayName,
                homeScore,
                awayScore,
                BuildMatchSignals(obj),
                [],
                []);
        }
    }

    private static IEnumerable<EaProbeMatchDataResponse> ExtractTrackerMatchesFromRoot(JsonElement root, string matchType, long? requestedClubId)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("matches", out var matches) ||
            matches.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        var category = matchType switch
        {
            "friendlyMatch" => "friendly",
            "leagueMatch" => "league",
            "playoffMatch" => "playoff",
            _ => "friendly"
        };

        if (!matches.TryGetProperty(category, out var selected) || selected.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in selected.EnumerateArray())
        {
            var match = TryExtractTrackerMatch(item, matchType, requestedClubId);
            if (match is not null)
            {
                yield return match;
            }
        }
    }

    private static EaProbeMatchDataResponse? TryExtractTrackerMatch(JsonElement obj, string fallbackMatchType, long? requestedClubId)
    {
        if (obj.ValueKind != JsonValueKind.Object ||
            !obj.TryGetProperty("clubs", out var clubs) ||
            clubs.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var participants = new List<(long? ClubId, string? Name, int? Score, string? MatchType)>();
        foreach (var club in clubs.EnumerateObject())
        {
            var clubData = club.Value;
            if (clubData.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var parsedClubId = long.TryParse(club.Name, out var clubIdFromKey) ? clubIdFromKey : FindLong(clubData, "clubId");
            string? name = null;
            if (clubData.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Object)
            {
                name = FindString(details, "name", "clubName");
            }

            name ??= FindString(clubData, "name", "clubName");
            participants.Add((parsedClubId, name, FindInt(clubData, "score", "goals"), FindString(clubData, "matchType", "type")));
        }

        if (participants.Count < 2)
        {
            return null;
        }

        var ordered = requestedClubId is null
            ? participants.Take(2).ToArray()
            : participants.OrderByDescending(participant => participant.ClubId == requestedClubId).Take(2).ToArray();

        var home = ordered[0];
        var away = ordered[1];

        return new EaProbeMatchDataResponse(
            FindString(obj, "matchId", "id", "gameId"),
            FindDate(obj, "timestamp", "date", "playedAt", "createdAt", "matchDate"),
            NormalizeMatchType(home.MatchType ?? away.MatchType) ?? fallbackMatchType,
            home.Name,
            away.Name,
            home.Score,
            away.Score,
            BuildMatchSignals(obj),
            ExtractTrackerClubStats(obj),
            ExtractTrackerPlayerStats(obj));
    }

    private static IReadOnlyCollection<EaMatchClubStatResponse> ExtractTrackerClubStats(JsonElement obj)
    {
        if (!obj.TryGetProperty("aggregate", out var aggregate) || aggregate.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var stats = new List<EaMatchClubStatResponse>();
        foreach (var club in aggregate.EnumerateObject())
        {
            if (!long.TryParse(club.Name, out var clubId) || club.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var passesMade = FindInt(club.Value, "passesmade");
            var passAttempts = FindInt(club.Value, "passattempts");
            var tacklesMade = FindInt(club.Value, "tacklesmade");
            var tackleAttempts = FindInt(club.Value, "tackleattempts");

            stats.Add(new EaMatchClubStatResponse(
                clubId,
                FindInt(club.Value, "goals") ?? FindInt(club.Value, "SCORE"),
                FindInt(club.Value, "assists"),
                FindDouble(club.Value, "rating"),
                FindInt(club.Value, "shots"),
                passesMade,
                passAttempts,
                Percent(passesMade, passAttempts),
                tacklesMade,
                tackleAttempts,
                Percent(tacklesMade, tackleAttempts),
                FindInt(club.Value, "saves"),
                FindInt(club.Value, "goalsconceded"),
                FindInt(club.Value, "redcards"),
                FindInt(club.Value, "mom")));
        }

        return stats;
    }

    private static IReadOnlyCollection<EaMatchPlayerStatResponse> ExtractTrackerPlayerStats(JsonElement obj)
    {
        if (!obj.TryGetProperty("players", out var players) || players.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var stats = new List<EaMatchPlayerStatResponse>();
        foreach (var clubPlayers in players.EnumerateObject())
        {
            if (!long.TryParse(clubPlayers.Name, out var clubId) || clubPlayers.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var player in clubPlayers.Value.EnumerateObject())
            {
                if (player.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var playerName = FindString(player.Value, "playername", "name");
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    continue;
                }

                var passesMade = FindInt(player.Value, "passesmade");
                var passAttempts = FindInt(player.Value, "passattempts");
                var tacklesMade = FindInt(player.Value, "tacklesmade");
                var tackleAttempts = FindInt(player.Value, "tackleattempts");

                stats.Add(new EaMatchPlayerStatResponse(
                    clubId,
                    player.Name,
                    playerName,
                    FindString(player.Value, "pos", "position"),
                    FindInt(player.Value, "goals"),
                    FindInt(player.Value, "assists"),
                    FindDouble(player.Value, "rating"),
                    FindInt(player.Value, "mom") == 1,
                    FindInt(player.Value, "shots"),
                    passesMade,
                    passAttempts,
                    Percent(passesMade, passAttempts),
                    tacklesMade,
                    tackleAttempts,
                    Percent(tacklesMade, tackleAttempts),
                    FindInt(player.Value, "saves"),
                    FindInt(player.Value, "goalsconceded"),
                    FindInt(player.Value, "secondsPlayed"),
                    FindString(player.Value, "vproattr"),
                    FindInt(player.Value, "cleansheetsany"),
                    FindInt(player.Value, "cleansheetsdef"),
                    FindInt(player.Value, "cleansheetsgk"),
                    FindInt(player.Value, "ballDiveSaves"),
                    FindInt(player.Value, "crossSaves"),
                    FindInt(player.Value, "goodDirectionSaves"),
                    FindInt(player.Value, "parrySaves"),
                    FindInt(player.Value, "punchSaves"),
                    FindInt(player.Value, "reflexSaves"),
                    FindInt(player.Value, "redcards"),
                    FindInt(player.Value, "gameTime"),
                    FindInt(player.Value, "realtimegame"),
                    FindInt(player.Value, "realtimeidle"),
                    FindInt(player.Value, "archetypeid"),
                    FindString(player.Value, "match_event_aggregate_0"),
                    FindString(player.Value, "match_event_aggregate_1"),
                    FindString(player.Value, "match_event_aggregate_2"),
                    FindString(player.Value, "match_event_aggregate_3")));
            }
        }

        return stats;
    }

    private static double? Percent(int? made, int? attempts)
    {
        return attempts > 0 && made is not null ? Math.Round((double)made.Value / attempts.Value * 100, 2) : null;
    }

    private static string? NormalizeMatchType(string? value)
    {
        return value switch
        {
            "1" => "leagueMatch",
            "5" => "friendlyMatch",
            "9" => "playoffMatch",
            _ => value
        };
    }

    private static IReadOnlyCollection<string> BuildMatchSignals(JsonElement obj)
    {
        var raw = obj.GetRawText();
        var signals = new List<string>();

        if (raw.Contains("assist", StringComparison.OrdinalIgnoreCase))
        {
            signals.Add("assists");
        }

        if (raw.Contains("motm", StringComparison.OrdinalIgnoreCase) || raw.Contains("playerOfTheMatch", StringComparison.OrdinalIgnoreCase))
        {
            signals.Add("player of the match");
        }

        if (raw.Contains("rating", StringComparison.OrdinalIgnoreCase))
        {
            signals.Add("notes joueurs");
        }

        if (raw.Contains("height", StringComparison.OrdinalIgnoreCase) || raw.Contains("taille", StringComparison.OrdinalIgnoreCase))
        {
            signals.Add("controle taille");
        }

        return signals;
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumerateObjects(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateObjects(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool HasAnyProperty(JsonElement obj, params string[] names)
    {
        return obj.ValueKind == JsonValueKind.Object &&
            obj.EnumerateObject().Any(property => names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? FindString(JsonElement obj, params string[] names)
    {
        if (obj.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in obj.EnumerateObject())
        {
            if (!names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            return property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        return null;
    }

    private static int? FindInt(JsonElement obj, params string[] names)
    {
        var value = FindNumberText(obj, names);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static long? FindLong(JsonElement obj, params string[] names)
    {
        var value = FindNumberText(obj, names);
        return long.TryParse(value, out var parsed) ? parsed : null;
    }

    private static double? FindDouble(JsonElement obj, params string[] names)
    {
        var value = FindNumberText(obj, names);
        return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static string? FindNumberText(JsonElement obj, params string[] names)
    {
        var value = FindString(obj, names);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Replace("%", string.Empty).Trim();
    }

    private static DateTimeOffset? FindDate(JsonElement obj, params string[] names)
    {
        var value = FindString(obj, names);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (long.TryParse(value, out var epoch))
        {
            return epoch > 9_999_999_999 ? DateTimeOffset.FromUnixTimeMilliseconds(epoch) : DateTimeOffset.FromUnixTimeSeconds(epoch);
        }

        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private static void AddInterestingFields(Dictionary<string, string?> fields, string source, JsonElement root)
    {
        var interesting = new[] { "division", "skill", "rating", "wins", "losses", "draws", "goals", "assists", "matches", "height", "pass", "tackle", "motm", "member", "position" };

        foreach (var obj in EnumerateObjects(root))
        {
            if (fields.Count >= 60 || obj.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var property in obj.EnumerateObject())
            {
                if (fields.Count >= 60)
                {
                    return;
                }

                if (!interesting.Any(token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase)) || !IsSimple(property.Value))
                {
                    continue;
                }

                var key = $"{source}.{property.Name}";
                fields.TryAdd(key, property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.GetRawText());
            }
        }
    }

    private static bool IsSimple(JsonElement element)
    {
        return element.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null;
    }

    private static string? CreatePreview(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        var compact = rawBody.Replace("\r", string.Empty).Replace("\n", " ").Trim();
        return compact.Length <= 1500 ? compact : compact[..1500];
    }

    private static long? TryExtractClubId(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return FindClubId(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static long? FindClubId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, "clubId", StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt64(out var number))
                    {
                        return number;
                    }

                    if (property.Value.ValueKind == JsonValueKind.String && long.TryParse(property.Value.GetString(), out var parsed))
                    {
                        return parsed;
                    }
                }

                var nested = FindClubId(property.Value);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindClubId(item);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private sealed record ProbeRawPayload(string Name, string Endpoint, string RawJson);
}
