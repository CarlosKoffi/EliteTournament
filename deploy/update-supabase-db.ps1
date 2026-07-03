param(
    [string]$HostName = "aws-0-eu-west-1.pooler.supabase.com",
    [string]$Database = "postgres",
    [string]$Username = "postgres.tfzvqmdwovhrmglfhkeu",
    [int]$Port = 5432
)

$ErrorActionPreference = "Stop"

$password = Read-Host "Supabase database password" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
$plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

$env:ConnectionStrings__Supabase = "Host=$HostName;Port=$Port;Database=$Database;Username=$Username;Password=$plainPassword;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;Command Timeout=30;Include Error Detail=true"
$env:Database__Provider = "Postgres"

dotnet ef database update --project src\CPElite.Infrastructure --startup-project src\CPElite.Api
