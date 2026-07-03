param(
    [string]$HostName = "db.tfzvqmdwovhrmglfhkeu.supabase.co",
    [int]$Port = 5432,
    [string]$Database = "postgres",
    [string]$Username = "postgres"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "TS Tournament - API locale avec Supabase" -ForegroundColor Green
Write-Host "Le mot de passe sera masque et ne sera pas sauvegarde dans le projet."

$securePassword = Read-Host "Mot de passe database Supabase" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Database__Provider = "Postgres"
$env:ConnectionStrings__Supabase = "Host=$HostName;Port=$Port;Database=$Database;Username=$Username;Password=$password;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;Command Timeout=30;Include Error Detail=true"
$env:Jwt__SigningKey = "local-development-signing-key-that-is-long-enough-for-tests"
$env:EaSync__Enabled = "false"
$env:EaMatchVerification__Enabled = "false"

Write-Host "Lancement API: http://localhost:5088" -ForegroundColor Green
Set-Location $projectRoot
dotnet run --project src\CPElite.Api --urls http://localhost:5088
