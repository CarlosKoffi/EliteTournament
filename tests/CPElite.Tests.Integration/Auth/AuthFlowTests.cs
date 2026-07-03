using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CPElite.Contracts.Auth;
using CPElite.Contracts.Common;
using CPElite.Contracts.Users;
using CPElite.Tests.Integration.Support;

namespace CPElite.Tests.Integration.Auth;

public sealed class AuthFlowTests : IClassFixture<ApiTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public AuthFlowTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_returns_token_that_can_access_me()
    {
        var auth = await _client.RegisterPlayerAsync($"player-{Guid.NewGuid():N}@test.com");
        _client.AuthorizeAs(auth);

        var me = await _client.GetFromJsonAsync<MeResponse>("/api/me", JsonOptions);

        Assert.NotNull(me);
        Assert.Equal(auth.User.Id, me!.User.Id);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@test.com";
        await _client.RegisterPlayerAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterPlayerRequest(email.ToUpperInvariant(), "Password123", "Duplicate", null, null, Platform.CrossPlay, "en", "UTC"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_rejects_wrong_password_with_unauthorized()
    {
        var email = $"login-{Guid.NewGuid():N}@test.com";
        await _client.RegisterPlayerAsync(email);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
