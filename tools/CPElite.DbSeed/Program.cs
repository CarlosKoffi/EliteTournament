using CPElite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var connectionString = GetOption(args, "--connection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Supabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Connection string manquante. Utilise --connection \"Host=...;Port=5432;Database=...;Username=...;Password=...\"");
    return 2;
}

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var deployDirectory = Path.Combine(repoRoot, "deploy");
var scripts = new[]
{
    Path.Combine(deployDirectory, "seed-thesurvivors-ea-matches.sql"),
    Path.Combine(deployDirectory, "seed-thesurvivors-past-tournament.sql")
};

foreach (var script in scripts)
{
    if (!File.Exists(script))
    {
        Console.Error.WriteLine($"Fichier SQL introuvable: {script}");
        return 3;
    }
}

var options = new DbContextOptionsBuilder<CPEliteDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new CPEliteDbContext(options);

foreach (var script in scripts)
{
    Console.WriteLine($"Execution seed: {Path.GetFileName(script)}");
    var sql = await File.ReadAllTextAsync(script);
    await db.Database.ExecuteSqlRawAsync(sql);
}

Console.WriteLine("Seed TheSurvivors termine: matchs EA + tournoi passe applicatif.");
return 0;

static string? GetOption(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static string FindRepoRoot(string startDirectory)
{
    var current = new DirectoryInfo(startDirectory);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "CPElite.sln")) &&
            Directory.Exists(Path.Combine(current.FullName, "deploy")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new DirectoryNotFoundException("Impossible de trouver la racine du repo CPElite.");
}
