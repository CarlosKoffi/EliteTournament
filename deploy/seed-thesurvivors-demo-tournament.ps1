param(
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
    [string]$DatabaseUrl = $env:DATABASE_URL
)

$ErrorActionPreference = "Stop"

function Convert-NpgsqlConnectionStringToPostgresUrl {
    param([Parameter(Mandatory = $true)][string]$InputConnectionString)

    $parts = @{}
    foreach ($segment in $InputConnectionString -split ";") {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        $pair = $segment -split "=", 2
        if ($pair.Length -eq 2) {
            $parts[$pair[0].Trim().ToLowerInvariant()] = $pair[1].Trim()
        }
    }

    $hostName = $parts["host"]
    $port = if ($parts.ContainsKey("port")) { $parts["port"] } else { "5432" }
    $database = $parts["database"]
    $username = $parts["username"]
    $password = $parts["password"]

    if ([string]::IsNullOrWhiteSpace($hostName) -or
        [string]::IsNullOrWhiteSpace($database) -or
        [string]::IsNullOrWhiteSpace($username) -or
        [string]::IsNullOrWhiteSpace($password)) {
        throw "ConnectionString invalide. Format attendu: Host=...;Port=5432;Database=...;Username=...;Password=..."
    }

    $encodedUser = [uri]::EscapeDataString($username)
    $encodedPassword = [uri]::EscapeDataString($password)
    $encodedDatabase = [uri]::EscapeDataString($database)

    return "postgresql://${encodedUser}:${encodedPassword}@${hostName}:${port}/${encodedDatabase}?sslmode=require"
}

$psql = Get-Command psql -ErrorAction SilentlyContinue
if ($null -eq $psql) {
    throw "psql est introuvable. Installe PostgreSQL client tools ou lance les deux fichiers SQL depuis l'editeur SQL de Supabase/Coolify."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$matchesSeed = Join-Path $scriptRoot "seed-thesurvivors-ea-matches.sql"
$tournamentSeed = Join-Path $scriptRoot "seed-thesurvivors-past-tournament.sql"

if (-not (Test-Path -LiteralPath $matchesSeed)) {
    throw "Fichier introuvable: $matchesSeed"
}

if (-not (Test-Path -LiteralPath $tournamentSeed)) {
    throw "Fichier introuvable: $tournamentSeed"
}

$postgresUrl = $DatabaseUrl
if ([string]::IsNullOrWhiteSpace($postgresUrl)) {
    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        throw "Ajoute -ConnectionString ou -DatabaseUrl. Exemple: -ConnectionString `"Host=...;Port=5432;Database=...;Username=...;Password=...`""
    }

    $postgresUrl = Convert-NpgsqlConnectionStringToPostgresUrl -InputConnectionString $ConnectionString
}

Write-Host "Seed 1/2 - Matchs amicaux EA TheSurvivors"
& $psql.Source $postgresUrl -v ON_ERROR_STOP=1 -f $matchesSeed

Write-Host "Seed 2/2 - Tournoi passe TheSurvivors"
& $psql.Source $postgresUrl -v ON_ERROR_STOP=1 -f $tournamentSeed

Write-Host "Seed termine. Rafraichis la page Equipe New et les pages tournoi."
