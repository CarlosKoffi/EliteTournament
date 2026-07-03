using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CPElite.Web;
using CPElite.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = ResolveApiBaseUrl(builder.Configuration["ApiBaseUrl"], builder.HostEnvironment.BaseAddress, builder.HostEnvironment.IsProduction());
Console.WriteLine($"TS Tournament boot - environment: {builder.HostEnvironment.Environment}");
Console.WriteLine($"TS Tournament boot - app base: {builder.HostEnvironment.BaseAddress}");
Console.WriteLine($"TS Tournament boot - api base: {apiBaseUrl}");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(_ => new ApiClient(apiBaseUrl));
builder.Services.AddScoped<AuthState>();

await builder.Build().RunAsync();

static string ResolveApiBaseUrl(string? configuredApiBaseUrl, string hostBaseAddress, bool isProduction)
{
    if (isProduction && IsLocalhost(configuredApiBaseUrl))
    {
        Console.Error.WriteLine($"Ignoring localhost ApiBaseUrl in production: {configuredApiBaseUrl}");
        return hostBaseAddress;
    }

    return string.IsNullOrWhiteSpace(configuredApiBaseUrl)
        ? hostBaseAddress
        : configuredApiBaseUrl.Trim();
}

static bool IsLocalhost(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return false;
    }

    return value.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        || value.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
