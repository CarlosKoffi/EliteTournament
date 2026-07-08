using System.Text.Json;
using CPElite.Application.Abstractions;
using CPElite.Contracts.Ea;

namespace CPElite.Application.Services;

public sealed class EaClubDiscoveryService
{
    private static readonly string[] PlayerFieldNames =
    [
        "name",
        "playername",
        "displayName",
        "personaName",
        "proName",
        "clubName",
        "position",
        "favoritePosition",
        "overall",
        "rating",
        "ratingAve",
        "gamesPlayed",
        "matches",
        "goals",
        "assists",
        "shots",
        "passesmade",
        "passattempts",
        "tacklesmade",
        "tackleattempts",
        "mom",
        "platform",
        "height",
        "weight"
    ];

    private readonly IEaDiagnosticsClient _client;
    private readonly ITeamRepository _teams;
    private readonly IUserRepository _users;

    public EaClubDiscoveryService(IEaDiagnosticsClient client, ITeamRepository teams, IUserRepository users)
    {
        _client = client;
        _teams = teams;
        _users = users;
    }

    public async Task<Result<EaClubSearchResponse>> SearchClubAsync(string clubName, string? platform, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clubName))
        {
            return Result<EaClubSearchResponse>.Failure(ErrorType.Validation, "ea.club_name_required", "Club name is required.");
        }

        var resolvedPlatform = string.IsNullOrWhiteSpace(platform) ? "common-gen5" : platform.Trim();
        var endpoint = $"https://proclubstracker.com/api/clubs/search?clubName={Uri.EscapeDataString(clubName.Trim())}&platform={Uri.EscapeDataString(resolvedPlatform)}";
        var response = await _client.ProbeAsync(endpoint, cancellationToken);
        if (!response.Success || string.IsNullOrWhiteSpace(response.RawBody))
        {
            return Result<EaClubSearchResponse>.Failure(ErrorType.Validation, "ea.club_search_failed", response.Error ?? "EA club search failed.");
        }

        var results = ParseClubResults(response.RawBody, resolvedPlatform).ToArray();
        for (var index = 0; index < results.Length; index++)
        {
            var appTeam = await _teams.GetByEaClubIdAsync(results[index].EaClubId, cancellationToken);
            if (appTeam is not null)
            {
                results[index] = results[index] with
                {
                    IsInApplication = true,
                    ApplicationTeamId = appTeam.Id,
                    ApplicationTeamName = appTeam.Name
                };
            }
        }

        return Result<EaClubSearchResponse>.Success(new EaClubSearchResponse(clubName.Trim(), resolvedPlatform, results));
    }

    public async Task<Result<EaPlayerLookupResponse>> SearchPlayerAsync(string eaSportsId, string? platform, long? clubId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eaSportsId))
        {
            return Result<EaPlayerLookupResponse>.Failure(ErrorType.Validation, "ea.player_id_required", "EA Sports ID is required.");
        }

        var resolvedPlatform = string.IsNullOrWhiteSpace(platform) ? "common-gen5" : platform.Trim();
        var cleanId = eaSportsId.Trim();
        if (clubId is not null)
        {
            var clubEndpoint = $"https://proclubstracker.com/api/clubs/{clubId.Value}?platform={Uri.EscapeDataString(resolvedPlatform)}";
            var clubResponse = await _client.ProbeAsync(clubEndpoint, cancellationToken);
            if (clubResponse.Success && !string.IsNullOrWhiteSpace(clubResponse.RawBody))
            {
                var clubFields = ExtractPlayerFields(clubResponse.RawBody, cleanId);
                if (clubFields.Count > 0)
                {
                    var enrichedFields = new Dictionary<string, string?>(clubFields, StringComparer.OrdinalIgnoreCase)
                    {
                        ["clubId"] = clubId.Value.ToString()
                    };

                    return Result<EaPlayerLookupResponse>.Success(new EaPlayerLookupResponse(cleanId, resolvedPlatform, true, "Pro Clubs Tracker club roster/matches", enrichedFields, clubResponse.RawBody, clubId));
                }
            }
        }

        var candidateEndpoints = new[]
        {
            $"https://proclubstracker.com/api/players/search?playerName={Uri.EscapeDataString(cleanId)}&platform={Uri.EscapeDataString(resolvedPlatform)}",
            $"https://proclubstracker.com/api/members/search?playerName={Uri.EscapeDataString(cleanId)}&platform={Uri.EscapeDataString(resolvedPlatform)}",
            $"https://proclubs.ea.com/api/fc/members/search?platform={Uri.EscapeDataString(resolvedPlatform)}&searchName={Uri.EscapeDataString(cleanId)}"
        };

        foreach (var endpoint in candidateEndpoints)
        {
            var response = await _client.ProbeAsync(endpoint, cancellationToken);
            if (!response.Success || string.IsNullOrWhiteSpace(response.RawBody))
            {
                continue;
            }

            var fields = ExtractPlayerFields(response.RawBody, cleanId);
            if (fields.Count == 0)
            {
                continue;
            }

            return Result<EaPlayerLookupResponse>.Success(new EaPlayerLookupResponse(cleanId, resolvedPlatform, true, endpoint.Contains("proclubstracker", StringComparison.OrdinalIgnoreCase) ? "Pro Clubs Tracker" : "EA FC Clubs", fields, response.RawBody, clubId));
        }

        return Result<EaPlayerLookupResponse>.Success(new EaPlayerLookupResponse(cleanId, resolvedPlatform, false, "None", new Dictionary<string, string?>(), null, clubId));
    }

    public async Task<Result<EaClubRosterResponse>> GetClubRosterAsync(long clubId, string? platform, CancellationToken cancellationToken = default)
    {
        if (clubId <= 0)
        {
            return Result<EaClubRosterResponse>.Failure(ErrorType.Validation, "ea.club_id_required", "EA club ID is required.");
        }

        var resolvedPlatform = string.IsNullOrWhiteSpace(platform) ? "common-gen5" : platform.Trim();
        var endpoint = $"https://proclubstracker.com/api/clubs/{clubId}?platform={Uri.EscapeDataString(resolvedPlatform)}";
        var response = await _client.ProbeAsync(endpoint, cancellationToken);
        if (!response.Success || string.IsNullOrWhiteSpace(response.RawBody))
        {
            return Result<EaClubRosterResponse>.Failure(ErrorType.Validation, "ea.club_roster_failed", response.Error ?? "EA club roster lookup failed.");
        }

        var activeRosterKeys = ExtractActiveRosterKeys(response.RawBody);
        var players = ParseClubRoster(response.RawBody, activeRosterKeys).ToArray();
        for (var index = 0; index < players.Length; index++)
        {
            var appUser = await _users.GetByEaIdentityAsync(players[index].EaPlayerId, players[index].PlayerName, players[index].ProName, cancellationToken);
            if (appUser is not null)
            {
                players[index] = players[index] with
                {
                    IsInApplication = true,
                    ApplicationUserId = appUser.Id,
                    ApplicationDisplayName = appUser.DisplayName,
                    ApplicationEaClubId = appUser.EaClubId,
                    ApplicationEaClubName = appUser.EaClubName
                };
            }
        }

        var clubName = TryReadClubName(response.RawBody);
        return Result<EaClubRosterResponse>.Success(new EaClubRosterResponse(clubId, resolvedPlatform, clubName, players));
    }

    private static IEnumerable<EaClubSearchResultResponse> ParseClubResults(string rawJson, string platform)
    {
        using var document = JsonDocument.Parse(rawJson);
        foreach (var item in EnumerateObjects(document.RootElement))
        {
            var clubId = FindLong(item, "clubId", "id");
            var name = FindString(item, "name", "clubName");
            if (clubId is null || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            yield return new EaClubSearchResultResponse(
                clubId.Value,
                name,
                FindString(item, "abbr", "abbreviation", "shortName"),
                platform,
                FindInt(item, "division", "currentDivision"),
                FindInt(item, "membersCount", "memberCount", "members"),
                "Pro Clubs Tracker");
        }
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

    private static string? FindString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static long? FindLong(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static int? FindInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string?> ExtractPlayerFields(string rawJson, string eaSportsId)
    {
        using var document = JsonDocument.Parse(rawJson);
        var directMatch = ExtractPlayerFieldsFromMatchingObject(document.RootElement, eaSportsId, requireDirectNameMatch: true);
        if (directMatch.Count > 0)
        {
            return directMatch;
        }

        return ExtractPlayerFieldsFromMatchingObject(document.RootElement, eaSportsId, requireDirectNameMatch: false);
    }

    private static IReadOnlyDictionary<string, string?> ExtractPlayerFieldsFromMatchingObject(JsonElement root, string eaSportsId, bool requireDirectNameMatch)
    {
        foreach (var item in EnumerateObjects(root))
        {
            var directNameMatch = new[] { "playername", "name", "displayName", "personaName", "proName", "gamertag" }
                .Select(name => FindString(item, name))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Any(value => value!.Contains(eaSportsId, StringComparison.OrdinalIgnoreCase));

            if (requireDirectNameMatch && !directNameMatch)
            {
                continue;
            }

            if (!requireDirectNameMatch && !directNameMatch && !JsonSerializer.Serialize(item).Contains(eaSportsId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in PlayerFieldNames)
            {
                if (item.TryGetProperty(name, out var value))
                {
                    fields[name] = ToDisplayValue(value);
                }
            }

            if (fields.Count > 0)
            {
                return fields;
            }
        }

        return new Dictionary<string, string?>();
    }

    private static IEnumerable<EaClubRosterPlayerResponse> ParseClubRoster(string rawJson, IReadOnlySet<string> activeRosterKeys)
    {
        using var document = JsonDocument.Parse(rawJson);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activeObjects = EnumerateActiveRosterObjects(document.RootElement).ToArray();
        var sourceObjects = activeObjects.Length > 0 ? activeObjects : EnumerateObjects(document.RootElement);

        foreach (var item in sourceObjects)
        {
            var playerName = FindString(item, "playername", "name", "displayName", "personaName", "gamertag", "proName");
            var eaPlayerId = FindString(item, "eaPlayerId", "playerId", "personaId", "id") ?? playerName;
            if (string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(eaPlayerId))
            {
                continue;
            }

            if (activeObjects.Length == 0 && !HasAnyPlayerStat(item))
            {
                continue;
            }

            if (!IsInActiveRoster(item, eaPlayerId, playerName, activeRosterKeys) || !seen.Add(eaPlayerId))
            {
                continue;
            }

            yield return new EaClubRosterPlayerResponse(
                eaPlayerId,
                playerName,
                FindString(item, "proName", "clubName"),
                FindString(item, "position", "favoritePosition"),
                FindInt(item, "overall", "rating"),
                FindInt(item, "height"),
                FindInt(item, "weight"),
                FindInt(item, "matches", "gamesPlayed"),
                FindInt(item, "goals"),
                FindInt(item, "assists"),
                FindDouble(item, "averageRating", "ratingAve", "rating"));
        }
    }

    private static bool HasAnyPlayerStat(JsonElement item)
    {
        return FindInt(item, "matches", "gamesPlayed", "goals", "assists", "overall", "height", "weight") is not null
            || FindDouble(item, "averageRating", "ratingAve", "rating") is not null;
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
        AddKey(keys, FindString(item, "playerId"));
        AddKey(keys, FindString(item, "personaId"));
        AddKey(keys, FindString(item, "eaPlayerId"));
        AddKey(keys, FindString(item, "id"));
        AddKey(keys, FindString(item, "personaName"));
        AddKey(keys, FindString(item, "eaSportsId"));
        AddKey(keys, FindString(item, "gamertag"));
        AddKey(keys, FindString(item, "playername"));
        AddKey(keys, FindString(item, "name"));
        AddKey(keys, FindString(item, "displayName"));
        AddKey(keys, FindString(item, "proName"));
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
            FindString(item, "playerId"),
            FindString(item, "personaId"),
            FindString(item, "eaPlayerId"),
            FindString(item, "id"),
            FindString(item, "personaName"),
            FindString(item, "eaSportsId"),
            FindString(item, "gamertag"),
            FindString(item, "playername"),
            FindString(item, "name"),
            FindString(item, "displayName"),
            FindString(item, "proName")
        };

        return candidates.Any(candidate => !string.IsNullOrWhiteSpace(candidate) && activeRosterKeys.Contains(candidate.Trim()));
    }

    private static void AddKey(HashSet<string> keys, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            keys.Add(value.Trim());
        }
    }

    private static string? TryReadClubName(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        foreach (var item in EnumerateObjects(document.RootElement))
        {
            var name = FindString(item, "name", "clubName");
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return null;
    }

    private static double? FindDouble(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? ToDisplayValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }
}
