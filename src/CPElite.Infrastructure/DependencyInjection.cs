using CPElite.Application.Abstractions;
using CPElite.Application.Services;
using CPElite.Infrastructure.BackgroundServices;
using CPElite.Infrastructure.Clients;
using CPElite.Infrastructure.Data;
using CPElite.Infrastructure.Repositories;
using CPElite.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CPElite.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCPEliteInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = configuration["Jwt:Issuer"] ?? options.Issuer;
            options.Audience = configuration["Jwt:Audience"] ?? options.Audience;
            options.SigningKey = configuration["Jwt:SigningKey"]
                ?? configuration["Jwt:Secret"]
                ?? options.SigningKey;

            if (int.TryParse(configuration["Jwt:ExpiryMinutes"], out var expiryMinutes))
            {
                options.ExpiryMinutes = expiryMinutes;
            }
        });

        var provider = configuration["Database:Provider"] ?? "Postgres";
        services.AddDbContext<CPEliteDbContext>(options =>
        {
            if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                var databaseName = configuration["Database:Name"] ?? "CPElite";
                options.UseInMemoryDatabase(databaseName);
                return;
            }

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("Supabase")
                ?? configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Supabase/Postgres connection string is not configured.");

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<IClubRankingRepository, ClubRankingRepository>();
        services.AddScoped<IEaApiCacheRepository, EaApiCacheRepository>();
        services.AddScoped<IEaSyncRepository, EaSyncRepository>();
        services.AddScoped<IEaDiagnosticsRepository, EaDiagnosticsRepository>();
        services.AddScoped<IAccessRepository, AccessRepository>();
        services.AddScoped<ITournamentParticipationRepository, TournamentParticipationRepository>();
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddHttpClient<DirectEaProClubsClient>(client =>
        {
            client.BaseAddress = new Uri(EnsureTrailingSlash(configuration["EaApi:BaseUrl"] ?? "https://proclubs.ea.com/api/fc"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CPElite/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddHttpClient<ProClubsTrackerClient>(client =>
        {
            client.BaseAddress = new Uri("https://proclubstracker.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CPElite/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddScoped<IEaProClubsClient, EaProClubsClient>();
        services.AddHttpClient<IEaDiagnosticsClient, EaDiagnosticsClient>(client =>
        {
            client.BaseAddress = new Uri(EnsureTrailingSlash(configuration["EaApi:BaseUrl"] ?? "https://proclubs.ea.com/api/fc"));
            client.DefaultRequestHeaders.TryAddWithoutValidation("authority", "proclubs.ea.com");
            client.DefaultRequestHeaders.TryAddWithoutValidation("accept-language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua", "\"Google Chrome\";v=\"141\", \"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"141\"");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(configuration["EaApi:UserAgent"] ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json,text/plain,*/*");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<AuthService>();
        services.AddScoped<TeamService>();
        services.AddScoped<TodayService>();
        services.AddScoped<EaClubDiscoveryService>();
        services.AddScoped<EaSyncService>();
        services.AddScoped<EaDiagnosticsService>();
        services.AddScoped<TournamentService>();
        services.AddScoped<ClubRankingService>();
        services.AddScoped<AccessService>();
        services.AddScoped<TournamentParticipationService>();
        services.AddScoped<ContentService>();
        services.AddHostedService<EaSyncBackgroundService>();
        services.AddHostedService<EaMatchVerificationBackgroundService>();

        return services;
    }

    private static string EnsureTrailingSlash(string url)
    {
        return url.EndsWith("/", StringComparison.Ordinal) ? url : $"{url}/";
    }
}
