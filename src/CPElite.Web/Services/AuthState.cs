using CPElite.Contracts.Auth;
using Microsoft.JSInterop;

namespace CPElite.Web.Services;

public sealed class AuthState
{
    private const string TokenKey = "cpelite.token";
    private const string UserKey = "cpelite.user";
    private readonly ApiClient _api;
    private readonly IJSRuntime _js;

    public AuthState(ApiClient api, IJSRuntime js)
    {
        _api = api;
        _js = js;
    }

    public UserSummaryResponse? User { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_api.Token);

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        var token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        var userJson = await _js.InvokeAsync<string?>("localStorage.getItem", UserKey);
        _api.Token = token;
        User = string.IsNullOrWhiteSpace(userJson)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<UserSummaryResponse>(userJson, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Changed?.Invoke();
    }

    public async Task SetSessionAsync(AuthResponse response)
    {
        _api.Token = response.AccessToken;
        User = response.User;
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, response.AccessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", UserKey, System.Text.Json.JsonSerializer.Serialize(response.User));
        Changed?.Invoke();
    }

    public async Task UpdateUserAsync(UserSummaryResponse user)
    {
        User = user;
        await _js.InvokeVoidAsync("localStorage.setItem", UserKey, System.Text.Json.JsonSerializer.Serialize(user));
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        _api.Token = null;
        User = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", UserKey);
        Changed?.Invoke();
    }
}
