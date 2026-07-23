using CPElite.Application.Abstractions;
using CPElite.Domain.Entities;
using CPElite.Domain.Enums;
using CPElite.Infrastructure.Data;
using System.Data;
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
        var environment = services.GetRequiredService<IWebHostEnvironment>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        try
        {
            logger.LogInformation("Applying EF Core migrations.");
            await dbContext.Database.MigrateAsync(cancellationToken);

            await SeedAdminUserAsync(dbContext, configuration, passwordHasher, logger, cancellationToken);
            await SeedLocalizedContentAsync(dbContext, logger, cancellationToken);
            await SeedTheSurvivorsDemoTournamentAsync(dbContext, environment, logger, cancellationToken);

            logger.LogInformation("Database initialization completed.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization failed. Check the PostgreSQL connection string, credentials, network access and migration permissions.");
            throw;
        }
    }

    private static async Task SeedTheSurvivorsDemoTournamentAsync(
        CPEliteDbContext dbContext,
        IWebHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var seedDirectory = Path.Combine(environment.ContentRootPath, "Seed");
        var scripts = new[]
        {
            Path.Combine(seedDirectory, "seed-thesurvivors-ea-matches.sql"),
            Path.Combine(seedDirectory, "seed-thesurvivors-past-tournament.sql")
        };

        foreach (var script in scripts)
        {
            if (!File.Exists(script))
            {
                throw new FileNotFoundException($"TheSurvivors seed script was not found in the published API output: {script}", script);
            }
        }

        foreach (var script in scripts)
        {
            logger.LogInformation("Running TheSurvivors seed script {ScriptName}.", Path.GetFileName(script));
            var sql = await File.ReadAllTextAsync(script, cancellationToken);
            await ExecuteSqlScriptAsync(dbContext, sql, cancellationToken);
        }

        logger.LogInformation("TheSurvivors demo tournament seed completed.");
    }

    private static async Task ExecuteSqlScriptAsync(CPEliteDbContext dbContext, string sql, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 300;
        await command.ExecuteNonQueryAsync(cancellationToken);
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
