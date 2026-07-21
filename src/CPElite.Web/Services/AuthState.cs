using CPElite.Contracts.Auth;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CPElite.Web.Services;

public sealed class AuthState
{
    private const string TokenKey = "cpelite.token";
    private const string UserKey = "cpelite.user";
    private readonly ApiClient _api;
    private readonly IJSRuntime _js;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

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
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            var userJson = await _js.InvokeAsync<string?>("localStorage.getItem", UserKey);
            _api.Token = token;
            User = DeserializeUser(userJson);

            if (!string.IsNullOrWhiteSpace(userJson) && User is null)
            {
                await ClearStoredSessionAsync();
            }

            if (!string.IsNullOrWhiteSpace(_api.Token))
            {
                var me = await _api.GetMeAsync();
                if (me.Value is not null)
                {
                    User = me.Value.User;
                    await _js.InvokeVoidAsync("localStorage.setItem", UserKey, JsonSerializer.Serialize(User, _jsonOptions));
                }
                else if (IsInvalidStoredSession(me.Error))
                {
                    Console.Error.WriteLine($"Stored auth session is no longer valid and will be cleared. {me.Error}");
                    _api.Token = null;
                    User = null;
                    await ClearStoredSessionAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Auth initialization failed. Falling back to guest session. {ex.Message}");
            _api.Token = null;
            User = null;
            await TryClearStoredSessionAsync();
        }

        NotifyChanged();
    }

    public async Task SetSessionAsync(AuthResponse response)
    {
        _api.Token = response.AccessToken;
        User = response.User;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, response.AccessToken);
            await _js.InvokeVoidAsync("localStorage.setItem", UserKey, JsonSerializer.Serialize(response.User, _jsonOptions));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to persist auth session in localStorage. {ex.Message}");
        }

        NotifyChanged();
    }

    public async Task UpdateUserAsync(UserSummaryResponse user)
    {
        User = user;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", UserKey, JsonSerializer.Serialize(user, _jsonOptions));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to persist user session in localStorage. {ex.Message}");
        }

        NotifyChanged();
    }

    public async Task SignOutAsync()
    {
        _api.Token = null;
        User = null;
        await TryClearStoredSessionAsync();
        NotifyChanged();
    }

    private UserSummaryResponse? DeserializeUser(string? userJson)
    {
        if (string.IsNullOrWhiteSpace(userJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<UserSummaryResponse>(userJson, _jsonOptions);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Stored auth user is invalid and will be cleared. {ex.Message}");
            return null;
        }
    }

    private async Task TryClearStoredSessionAsync()
    {
        try
        {
            await ClearStoredSessionAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to clear auth session from localStorage. {ex.Message}");
        }
    }

    private async Task ClearStoredSessionAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", UserKey);
    }

    private static bool IsInvalidStoredSession(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        return error.Contains("User was not found", StringComparison.OrdinalIgnoreCase)
            || error.Contains("user.not_found", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
            || error.Contains("401", StringComparison.OrdinalIgnoreCase);
    }

    private void NotifyChanged()
    {
        try
        {
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Auth state change notification failed. {ex.Message}");
        }
    }
}
