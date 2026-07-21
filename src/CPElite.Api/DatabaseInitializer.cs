using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Domain.Enums;
using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CPElite.Api;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var dbContext = services.GetRequiredService<CPEliteDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        try
        {
            logger.LogInformation("Applying EF Core migrations.");
            await dbContext.Database.MigrateAsync(cancellationToken);

            await RunStartupCleanupAsync(dbContext, configuration, logger, cancellationToken);
            await SeedAdminUserAsync(dbContext, configuration, passwordHasher, logger, cancellationToken);
            await SeedLocalizedContentAsync(dbContext, logger, cancellationToken);

            logger.LogInformation("Database initialization completed.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization failed. Check the PostgreSQL connection string, credentials, network access and migration permissions.");
            throw;
        }
    }

    private static async Task RunStartupCleanupAsync(CPEliteDbContext dbContext, IConfiguration configuration, ILogger logger, CancellationToken cancellationToken)
    {
        var cleanupRegisteredUsers = configuration.GetValue("StartupCleanup:RegisteredUsers", false);
        var cleanupContentOnly = configuration.GetValue("StartupCleanup:ContentOnlyReset", false);

        if (cleanupContentOnly)
        {
            logger.LogWarning("Startup cleanup ContentOnlyReset is enabled. Users, teams, tournaments and EA cached data will be deleted.");
            await dbContext.Database.ExecuteSqlRawAsync("""
                truncate table
                    "TournamentMoments",
                    "MatchScoreSubmissions",
                    "TournamentPlayerConfirmations",
                    "TournamentMatches",
                    "TournamentRegistrations",
                    "ChampionTitles",
                    "Tournaments",
                    "UserTournamentAccesses",
                    "TeamSlotAssignments",
                    "TeamSlotPackages",
                    "TeamPlayerDemands",
                    "TeamScheduleSlots",
                    "TeamPositions",
                    "TeamJoinRequests",
                    "TeamMembers",
                    "Teams",
                    "EaMatchPlayerStats",
                    "EaMatchClubStats",
                    "EaFriendlyMatches",
                    "EaPlayerProfileSnapshots",
                    "EaMatchSnapshots",
                    "EaMemberStatsSnapshots",
                    "EaClubSnapshots",
                    "EaApiCacheEntries",
                    "EaDiagnosticProbes",
                    "Users"
                restart identity cascade;
                """, cancellationToken);
            logger.LogWarning("Startup cleanup ContentOnlyReset completed. Remove StartupCleanup__ContentOnlyReset after this deployment.");
            return;
        }

        if (!cleanupRegisteredUsers)
        {
            return;
        }

        logger.LogWarning("Startup cleanup RegisteredUsers is enabled. Registered users and user-owned data will be deleted.");
        await dbContext.Database.ExecuteSqlRawAsync("""
            truncate table
                "TournamentPlayerConfirmations",
                "UserTournamentAccesses",
                "TeamSlotAssignments",
                "TeamSlotPackages",
                "TeamPlayerDemands",
                "TeamJoinRequests",
                "TeamMembers",
                "Users"
            restart identity cascade;
            """, cancellationToken);
        logger.LogWarning("Startup cleanup RegisteredUsers completed. Remove StartupCleanup__RegisteredUsers after this deployment.");
    }

    private static async Task SeedAdminUserAsync(CPEliteDbContext dbContext, IConfiguration configuration, IPasswordHasher passwordHasher, ILogger logger, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(user => user.IsAdmin, cancellationToken))
        {
            logger.LogInformation("Admin seed skipped because an admin user already exists.");
            return;
        }

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No admin user exists and Seed__AdminEmail/Seed__AdminPassword are not configured. The app will start without an admin account.");
            return;
        }

        if (!email.Contains('@', StringComparison.Ordinal) || password.Length < 12)
        {
            throw new InvalidOperationException("Seed admin configuration is invalid. Seed__AdminEmail must be an email and Seed__AdminPassword must contain at least 12 characters.");
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var existingUser = await dbContext.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            logger.LogWarning("A user with Seed__AdminEmail already exists but is not admin. Admin seed skipped to avoid modifying an existing account unexpectedly.");
            return;
        }

        var admin = new User(
            Guid.NewGuid(),
            email.Trim(),
            normalizedEmail,
            passwordHasher.Hash(password),
            "EliteTournament Admin",
            gamertag: null,
            eaSportsId: null,
            discordUserId: null,
            Platform.CrossPlay,
            "fr",
            "Europe/Zurich",
            DateTimeOffset.UtcNow,
            isAdmin: true);

        await dbContext.Users.AddAsync(admin, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Initial admin user created from Seed__AdminEmail.");
    }

    private static async Task SeedLocalizedContentAsync(CPEliteDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var seededCount = 0;

        foreach (var item in MinimalLocalizedContent)
        {
            var exists = await dbContext.LocalizedContents.AnyAsync(content => content.Key == item.Key && content.Language == item.Language, cancellationToken);
            if (exists)
            {
                continue;
            }

            await dbContext.LocalizedContents.AddAsync(new LocalizedContent(Guid.NewGuid(), item.Key, item.Language, item.Value, item.Section, item.Description, now, updatedByUserId: null), cancellationToken);
            seededCount++;
        }

        if (seededCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Localized content seed completed. {SeededCount} item(s) added.", seededCount);
    }

    private static readonly IReadOnlyCollection<SeedContentItem> MinimalLocalizedContent =
    [
        new("brand.name", "fr", "TS Tournament", "Global", "Nom affiche de l'application"),
        new("brand.name", "en", "TS Tournament", "Global", "Displayed application name"),
        new("nav.home", "fr", "Accueil", "Navigation", "Lien menu accueil"),
        new("nav.home", "en", "Home", "Navigation", "Home navigation link"),
        new("nav.tournaments", "fr", "Tournois", "Navigation", "Lien menu tournois"),
        new("nav.tournaments", "en", "Tournaments", "Navigation", "Tournaments navigation link"),
        new("nav.mercato", "fr", "Mercato", "Navigation", "Lien menu mercato"),
        new("nav.mercato", "en", "Market", "Navigation", "Market navigation link"),
        new("nav.my_team", "fr", "Mon equipe", "Navigation", "Lien menu equipe"),
        new("nav.my_team", "en", "My team", "Navigation", "My team navigation link"),
        new("nav.my_account", "fr", "Mon compte", "Navigation", "Lien menu compte"),
        new("nav.my_account", "en", "My account", "Navigation", "My account navigation link"),
        new("nav.admin", "fr", "Admin", "Navigation", "Lien menu admin"),
        new("nav.admin", "en", "Admin", "Navigation", "Admin navigation link"),
        new("home.hero.title", "fr", "Organise ta soiree Pro Clubs.", "Accueil", "Titre hero accueil"),
        new("home.hero.title", "en", "Run your Pro Clubs night.", "Home", "Home hero title"),
        new("account.page.title_guest", "fr", "Creer un compte", "Compte", "Titre compte invite"),
        new("account.page.title_guest", "en", "Create an account", "Account", "Guest account title"),
        new("admin.content.page.title", "fr", "Textes traduisibles", "Admin textes", "Titre admin contenu"),
        new("admin.content.page.title", "en", "Translatable texts", "Admin texts", "Content admin title")
    ];

    private sealed record SeedContentItem(string Key, string Language, string Value, string Section, string Description);
}
