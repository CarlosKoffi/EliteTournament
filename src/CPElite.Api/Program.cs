using System.Text;
using System.Text.Json.Serialization;
using CPElite.Api;
using CPElite.Infrastructure;
using CPElite.Infrastructure.Data;
using CPElite.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddCPEliteInfrastructure(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        var configuredOrigins = ReadConfiguredOrigins(builder.Configuration);
        var origins = configuredOrigins
            .Concat(builder.Environment.IsDevelopment() ? ["http://localhost:5041", "https://localhost:7003"] : [])
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
LogWebHostStartupInfo(app, startupLogger);

if (app.Environment.IsProduction())
{
    await app.InitializeDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/", () => Results.Redirect("/diagnostics/ea"));
    app.MapGet("/diagnostics/ea", () => Results.Content(EaDiagnosticsPage.Html, "text/html"));
}

app.UseCors("WebClient");
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = CreateBlazorContentTypeProvider()
});
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "CPElite.Api",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/health/db", async (CPEliteDbContext dbContext, CancellationToken cancellationToken) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "ok", database = "connected", utc = DateTimeOffset.UtcNow })
        : Results.Problem("Database connection failed.");
});

app.MapControllers();

var hasPublishedBlazorApp = HasPublishedBlazorApp(app.Environment.WebRootPath);
if (!app.Environment.IsDevelopment() && hasPublishedBlazorApp)
{
    app.MapFallbackToFile("index.html");
    startupLogger.LogInformation("Blazor fallback enabled with wwwroot/index.html.");
}
else if (!app.Environment.IsDevelopment())
{
    startupLogger.LogWarning("Blazor fallback disabled because wwwroot/index.html was not found.");
}

app.Lifetime.ApplicationStarted.Register(() => LogApplicationStarted(app, startupLogger));
app.Run();

static string[] ReadConfiguredOrigins(IConfiguration configuration)
{
    var sectionOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    var flatOrigins = configuration["Cors:AllowedOrigins"];

    if (string.IsNullOrWhiteSpace(flatOrigins))
    {
        return sectionOrigins;
    }

    return sectionOrigins
        .Concat(flatOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .ToArray();
}

static bool HasPublishedBlazorApp(string? webRootPath)
{
    if (string.IsNullOrWhiteSpace(webRootPath))
    {
        return false;
    }

    return File.Exists(Path.Combine(webRootPath, "index.html"));
}

static FileExtensionContentTypeProvider CreateBlazorContentTypeProvider()
{
    var provider = new FileExtensionContentTypeProvider();
    provider.Mappings[".dat"] = "application/octet-stream";
    provider.Mappings[".wasm"] = "application/wasm";
    provider.Mappings[".dll"] = "application/octet-stream";
    provider.Mappings[".br"] = "application/octet-stream";
    provider.Mappings[".gz"] = "application/gzip";
    return provider;
}

static void LogWebHostStartupInfo(WebApplication app, ILogger logger)
{
    var webRootPath = app.Environment.WebRootPath;
    var indexPath = string.IsNullOrWhiteSpace(webRootPath)
        ? null
        : Path.Combine(webRootPath, "index.html");

    logger.LogInformation("Environment: {EnvironmentName}", app.Environment.EnvironmentName);
    logger.LogInformation("ContentRootPath: {ContentRootPath}", app.Environment.ContentRootPath);
    logger.LogInformation("WebRootPath: {WebRootPath}", webRootPath ?? "(null)");
    logger.LogInformation("Blazor index path: {IndexPath}", indexPath ?? "(null)");
    logger.LogInformation("Blazor index exists: {IndexExists}", indexPath is not null && File.Exists(indexPath));
    LogStaticFile(logger, webRootPath, "_framework/blazor.webassembly.js");
    LogStaticFile(logger, webRootPath, "_framework/blazor.boot.json");
    LogStaticFile(logger, webRootPath, "CPElite.Web.styles.css");
    logger.LogInformation("ASPNETCORE_URLS: {AspNetCoreUrls}", Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "(not set)");
    logger.LogInformation("ASPNETCORE_HTTP_PORTS: {AspNetCoreHttpPorts}", Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "(not set)");
    logger.LogInformation("PORT: {Port}", Environment.GetEnvironmentVariable("PORT") ?? "(not set)");
}

static void LogStaticFile(ILogger logger, string? webRootPath, string relativePath)
{
    var path = string.IsNullOrWhiteSpace(webRootPath)
        ? null
        : Path.Combine(webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    logger.LogInformation("Static asset {RelativePath} exists: {Exists}", relativePath, path is not null && File.Exists(path));
}

static void LogApplicationStarted(WebApplication app, ILogger logger)
{
    var urls = app.Urls.Count == 0
        ? "(no urls reported by Kestrel)"
        : string.Join(", ", app.Urls);

    logger.LogInformation("Application started. Listening URLs: {Urls}", urls);
}

public partial class Program;
