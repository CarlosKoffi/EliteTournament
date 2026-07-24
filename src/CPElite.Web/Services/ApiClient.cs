using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CPElite.Contracts.Auth;
using CPElite.Contracts.Ea;
using CPElite.Contracts.Content;
using CPElite.Contracts.Rankings;
using CPElite.Contracts.Teams;
using CPElite.Contracts.Today;
using CPElite.Contracts.TournamentParticipation;
using CPElite.Contracts.Tournaments;
using CPElite.Contracts.Users;

namespace CPElite.Web.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(string apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentException("API base URL is required.", nameof(apiBaseUrl));
        }

        _http = new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public string? Token { get; set; }

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterPlayerRequest request) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", request, requiresAuth: false);

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request, requiresAuth: false);

    public Task<ApiResult<MeResponse>> GetMeAsync() =>
        SendAsync<MeResponse>(HttpMethod.Get, "api/me");

    public Task<ApiResult<ContentCatalogResponse>> GetContentCatalogAsync(string language = "fr") =>
        SendAsync<ContentCatalogResponse>(HttpMethod.Get, $"api/content?language={Uri.EscapeDataString(language)}", requiresAuth: false);

    public Task<ApiResult<ContentItemResponse>> UpsertContentItemAsync(UpsertContentItemRequest request) =>
        SendAsync<ContentItemResponse>(HttpMethod.Put, "api/content/admin", request);

    public Task<ApiResult<IReadOnlyCollection<ClubRankingResponse>>> GetClubRankingsAsync(int take = 5) =>
        SendAsync<IReadOnlyCollection<ClubRankingResponse>>(HttpMethod.Get, $"api/rankings/clubs?take={take}", requiresAuth: false);

    public Task<ApiResult<UserSummaryResponse>> UpdatePlayerProfileAsync(UpdatePlayerProfileRequest request) =>
        SendAsync<UserSummaryResponse>(HttpMethod.Patch, "api/me/profile", request);

    public Task<ApiResult<TeamAssetUploadResponse>> UploadProfilePhotoAsync(Stream stream, string fileName, string contentType) =>
        UploadImageAsync("api/me/profile-photo", stream, fileName, contentType);

    public Task<ApiResult<TeamResponse>> CreateTeamAsync(CreateTeamRequest request) =>
        SendAsync<TeamResponse>(HttpMethod.Post, "api/teams", request);

    public Task<ApiResult<TeamResponse>> GetTeamAsync(Guid teamId) =>
        SendAsync<TeamResponse>(HttpMethod.Get, $"api/teams/{teamId}");

    public Task<ApiResult<TeamResponse>> UpdateTeamProfileAsync(Guid teamId, UpdateTeamProfileRequest request) =>
        SendAsync<TeamResponse>(HttpMethod.Patch, $"api/teams/{teamId}/profile", request);

    public Task<ApiResult<TeamResponse>> JoinExistingTeamAsync(Guid teamId) =>
        SendAsync<TeamResponse>(HttpMethod.Post, $"api/teams/{teamId}/join-existing");
    public Task<ApiResult<TeamResponse>> JoinTeamAsync(JoinTeamRequest request) =>
        SendAsync<TeamResponse>(HttpMethod.Post, "api/teams/join", request);

    public Task<ApiResult<IReadOnlyCollection<TeamMemberResponse>>> GetTeamMembersAsync(Guid teamId) =>
        SendAsync<IReadOnlyCollection<TeamMemberResponse>>(HttpMethod.Get, $"api/teams/{teamId}/members");

    public Task<ApiResult<TeamTournamentSummaryResponse>> GetTeamTournamentSummaryAsync(Guid teamId) =>
        SendAsync<TeamTournamentSummaryResponse>(HttpMethod.Get, $"api/teams/{teamId}/tournament-summary");

    public Task<ApiResult<IReadOnlyCollection<TeamManagerClaimResponse>>> GetTeamManagerClaimsAsync(Guid teamId) =>
        SendAsync<IReadOnlyCollection<TeamManagerClaimResponse>>(HttpMethod.Get, $"api/teams/{teamId}/manager-claims");

    public Task<ApiResult<TeamManagerClaimResponse>> RequestTeamManagerClaimAsync(Guid teamId) =>
        SendAsync<TeamManagerClaimResponse>(HttpMethod.Post, $"api/teams/{teamId}/manager-claims");

    public Task<ApiResult<TeamManagerClaimResponse>> ApproveTeamManagerClaimAsync(Guid teamId, Guid claimId) =>
        SendAsync<TeamManagerClaimResponse>(HttpMethod.Post, $"api/teams/{teamId}/manager-claims/{claimId}/approve");

    public async Task<ApiResult<TeamAssetUploadResponse>> UploadTeamAssetAsync(Stream stream, string fileName, string contentType)
    {
        return await UploadImageAsync("api/team-assets/upload", stream, fileName, contentType);
    }

    private async Task<ApiResult<TeamAssetUploadResponse>> UploadImageAsync(string path, Stream stream, string fileName, string contentType)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (!string.IsNullOrWhiteSpace(Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        request.Content = content;

        try
        {
            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<TeamAssetUploadResponse>.Fail(ReadProblem(text, response.ReasonPhrase));
            }

            var value = JsonSerializer.Deserialize<TeamAssetUploadResponse>(text, _jsonOptions);
            return value is null ? ApiResult<TeamAssetUploadResponse>.Fail("Reponse vide de l'API.") : ApiResult<TeamAssetUploadResponse>.Ok(value);
        }
        catch (Exception ex)
        {
            return ApiResult<TeamAssetUploadResponse>.Fail(ex.Message);
        }
    }

    public Task<ApiResult<TodayResponse>> GetTodayAsync() =>
        SendAsync<TodayResponse>(HttpMethod.Get, "api/today");

    public Task<ApiResult<PlayerDemandResponse>> CreatePlayerDemandAsync(Guid teamId, CreatePlayerDemandRequest request) =>
        SendAsync<PlayerDemandResponse>(HttpMethod.Post, $"api/teams/{teamId}/player-demands", request);

    public Task<ApiResult<TournamentResponse>> CreateTournamentAsync(CreateTournamentRequest request) =>
        SendAsync<TournamentResponse>(HttpMethod.Post, "api/tournaments/official", request);

    public Task<ApiResult<TeamAssetUploadResponse>> UploadTournamentBannerAsync(Stream stream, string fileName, string contentType) =>
        UploadImageAsync("api/tournament-assets/banner", stream, fileName, contentType);

    public Task<ApiResult<IReadOnlyCollection<TournamentResponse>>> GetTournamentsAsync() =>
        SendAsync<IReadOnlyCollection<TournamentResponse>>(HttpMethod.Get, "api/tournaments", requiresAuth: false);

    public Task<ApiResult<TournamentAdminDetailResponse>> GetTournamentAdminDetailAsync(Guid tournamentId) =>
        SendAsync<TournamentAdminDetailResponse>(HttpMethod.Get, $"api/tournaments/{tournamentId}/admin-detail");

    public Task<ApiResult<TournamentRegistrationResponse>> RegisterTeamForTournamentAsync(Guid tournamentId, RegisterTeamForTournamentRequest request) =>
        SendAsync<TournamentRegistrationResponse>(HttpMethod.Post, $"api/tournaments/{tournamentId}/registrations", request);

    public Task<ApiResult<TournamentRegistrationSummaryResponse>> GetTournamentBoardAsync(Guid tournamentId) =>
        SendAsync<TournamentRegistrationSummaryResponse>(HttpMethod.Get, $"api/tournaments/{tournamentId}/registrations");

    public Task<ApiResult<IReadOnlyCollection<TournamentRegistrationEventResponse>>> GetTournamentRegistrationEventsAsync(Guid tournamentId, Guid? teamId = null)
    {
        var url = $"api/tournaments/{tournamentId}/registrations/events";
        if (teamId.HasValue)
        {
            url += $"?teamId={teamId.Value}";
        }

        return SendAsync<IReadOnlyCollection<TournamentRegistrationEventResponse>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<TournamentRegistrationDraftResponse>> GetTournamentRegistrationDraftAsync(Guid tournamentId, Guid? teamId = null)
    {
        var url = $"api/tournaments/{tournamentId}/registrations/draft";
        if (teamId.HasValue)
        {
            url += $"?teamId={teamId.Value}";
        }

        return SendAsync<TournamentRegistrationDraftResponse>(HttpMethod.Get, url);
    }

    public Task<ApiResult<TournamentRegistrationDraftResponse>> SaveTournamentRegistrationDraftAsync(Guid tournamentId, SaveTournamentRegistrationDraftRequest request) =>
        SendAsync<TournamentRegistrationDraftResponse>(HttpMethod.Put, $"api/tournaments/{tournamentId}/registrations/draft", request);

    public Task<ApiResult<TournamentRegistrationReplacementResponse>> WithdrawTournamentRegistrationAsync(Guid tournamentId, Guid teamId) =>
        SendAsync<TournamentRegistrationReplacementResponse>(HttpMethod.Post, $"api/tournaments/{tournamentId}/registrations/{teamId}/withdraw");

    public Task<ApiResult<DiscordTournamentRegistrationResponse>> PromoteNextWaitlistedTournamentTeamAsync(Guid tournamentId) =>
        SendAsync<DiscordTournamentRegistrationResponse>(HttpMethod.Post, $"api/tournaments/{tournamentId}/registrations/waitlist/promote-next");

    public Task<ApiResult<TournamentDrawResponse>> LaunchWorldCupTournamentAsync(Guid tournamentId, LaunchWorldCupTournamentRequest request) =>
        SendAsync<TournamentDrawResponse>(HttpMethod.Post, $"api/tournaments/{tournamentId}/launch-world-cup", request);

    public Task<ApiResult<IReadOnlyCollection<EaMatchVerificationResponse>>> RecoverTournamentScoresAsync(Guid tournamentId) =>
        SendAsync<IReadOnlyCollection<EaMatchVerificationResponse>>(HttpMethod.Post, $"api/tournaments/{tournamentId}/scores/recover");

    public Task<ApiResult<IReadOnlyCollection<TournamentScoreAuditResponse>>> GetTournamentScoreAuditsAsync(Guid tournamentId) =>
        SendAsync<IReadOnlyCollection<TournamentScoreAuditResponse>>(HttpMethod.Get, $"api/tournaments/{tournamentId}/scores/audits");

    public Task<ApiResult<IReadOnlyCollection<TournamentParticipantResponse>>> GetTournamentParticipantsAsync(Guid tournamentId, Guid teamId) =>
        SendAsync<IReadOnlyCollection<TournamentParticipantResponse>>(HttpMethod.Get, $"api/tournaments/{tournamentId}/teams/{teamId}/participants");

    public Task<ApiResult<TournamentParticipantResponse>> AddTournamentParticipantAsync(Guid tournamentId, Guid teamId, AddTournamentParticipantRequest request) =>
        SendAsync<TournamentParticipantResponse>(HttpMethod.Post, $"api/tournaments/{tournamentId}/teams/{teamId}/participants", request);

    public Task<ApiResult<TournamentParticipantReminderResponse>> RemindPendingTournamentParticipantsAsync(Guid tournamentId, Guid teamId) =>
        SendAsync<TournamentParticipantReminderResponse>(HttpMethod.Post, $"api/tournaments/{tournamentId}/teams/{teamId}/participants/remind-pending");

    public Task<ApiResult<EaProbeResponse>> ProbeEaAsync(EaProbeRequest request) =>
        SendAsync<EaProbeResponse>(HttpMethod.Post, "api/ea/diagnostics/probe", request);

    public Task<ApiResult<EaClubSearchResponse>> SearchEaClubsAsync(string clubName, string platform = "common-gen5") =>
        SendAsync<EaClubSearchResponse>(HttpMethod.Get, $"api/ea/clubs/search?clubName={Uri.EscapeDataString(clubName)}&platform={Uri.EscapeDataString(platform)}", requiresAuth: false);

    public Task<ApiResult<EaClubRosterResponse>> GetEaClubRosterAsync(long clubId, string platform = "common-gen5") =>
        SendAsync<EaClubRosterResponse>(HttpMethod.Get, $"api/ea/clubs/{clubId}/roster?platform={Uri.EscapeDataString(platform)}", requiresAuth: false);

    public Task<ApiResult<EaPlayerLookupResponse>> SearchEaPlayerAsync(string eaSportsId, string platform = "common-gen5", long? clubId = null)
    {
        var clubQuery = clubId is null ? string.Empty : $"&clubId={clubId.Value}";
        return SendAsync<EaPlayerLookupResponse>(HttpMethod.Get, $"api/ea/players/search?eaSportsId={Uri.EscapeDataString(eaSportsId)}&platform={Uri.EscapeDataString(platform)}{clubQuery}");
    }

    public Task<ApiResult<EaSyncResponse>> SyncTeamEaAsync(Guid teamId, bool forceRefresh = false) =>
        SendAsync<EaSyncResponse>(HttpMethod.Post, $"api/teams/{teamId}/ea/sync?force={forceRefresh.ToString().ToLowerInvariant()}");

    public Task<ApiResult<EaClubSnapshotResponse>> GetTeamEaClubAsync(Guid teamId) =>
        SendAsync<EaClubSnapshotResponse>(HttpMethod.Get, $"api/teams/{teamId}/ea/club");

    public Task<ApiResult<EaMemberStatsSummaryResponse>> GetTeamEaMembersAsync(Guid teamId) =>
        SendAsync<EaMemberStatsSummaryResponse>(HttpMethod.Get, $"api/teams/{teamId}/ea/members");

    public Task<ApiResult<IReadOnlyCollection<EaPlayerProfileSnapshotResponse>>> GetTeamEaPlayersAsync(Guid teamId) =>
        SendAsync<IReadOnlyCollection<EaPlayerProfileSnapshotResponse>>(HttpMethod.Get, $"api/teams/{teamId}/ea/players");

    public Task<ApiResult<IReadOnlyCollection<EaTournamentPlayerStatsResponse>>> GetTeamEaTournamentPlayerStatsAsync(Guid teamId) =>
        SendAsync<IReadOnlyCollection<EaTournamentPlayerStatsResponse>>(HttpMethod.Get, $"api/teams/{teamId}/ea/tournament-player-stats");

    public Task<ApiResult<IReadOnlyCollection<EaFriendlyMatchResponse>>> GetTeamEaFriendliesAsync(Guid teamId, int take = 20, Guid? tournamentMatchId = null)
    {
        var url = $"api/teams/{teamId}/ea/friendlies?take={take}";
        if (tournamentMatchId.HasValue)
        {
            url += $"&tournamentMatchId={tournamentMatchId.Value}";
        }

        return SendAsync<IReadOnlyCollection<EaFriendlyMatchResponse>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<EaFriendlyMatchDetailsResponse>> GetTeamEaFriendlyDetailsAsync(Guid teamId, string eaMatchId) =>
        SendAsync<EaFriendlyMatchDetailsResponse>(HttpMethod.Get, $"api/teams/{teamId}/ea/friendlies/{Uri.EscapeDataString(eaMatchId)}");

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null, bool requiresAuth = true)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: _jsonOptions);
        }

        if (requiresAuth && !string.IsNullOrWhiteSpace(Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        try
        {
            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Fail(ReadProblem(text, response.ReasonPhrase));
            }

            var value = JsonSerializer.Deserialize<T>(text, _jsonOptions);
            return value is null ? ApiResult<T>.Fail("Réponse vide de l'API.") : ApiResult<T>.Ok(value);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"API call failed: {method} {path} - {ex.Message}");
            return ApiResult<T>.Fail(ex.Message);
        }
    }

    private static string ReadProblem(string text, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback ?? "Erreur API.";
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.GetString() ?? text;
            }

            if (document.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? text;
            }
        }
        catch
        {
            return text;
        }

        return text;
    }
}
