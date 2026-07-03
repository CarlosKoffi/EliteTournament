using System.Text;
using System.Text.Json.Serialization;
using CPElite.Api;
using CPElite.Infrastructure;
using CPElite.Infrastructure.Data;
using CPElite.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
app.UseStaticFiles();
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

if (!app.Environment.IsDevelopment() && HasPublishedBlazorApp(app.Environment.WebRootPath))
{
    app.MapFallbackToFile("index.html");
}

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

public partial class Program;
