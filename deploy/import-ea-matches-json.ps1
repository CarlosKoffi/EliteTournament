param(
    [string]$ApiBaseUrl = "http://localhost:5088",
    [string]$JsonPath = "C:\Users\bruce\Downloads\matches.json",
    [long]$EaClubId = 2148207,
    [string]$Platform = "common-gen5",
    [string]$Token,
    [string]$Email,
    [string]$Password
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $JsonPath)) {
    throw "JSON file not found: $JsonPath"
}

$baseUrl = $ApiBaseUrl.TrimEnd("/")
$headers = @{}

if ([string]::IsNullOrWhiteSpace($Token)) {
    if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
        throw "Provide either -Token or both -Email and -Password."
    }

    $loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$baseUrl/api/auth/login" -ContentType "application/json" -Body $loginBody
    $Token = $login.accessToken

    if ([string]::IsNullOrWhiteSpace($Token)) {
        throw "Login succeeded but no access token was returned."
    }
}

$headers["Authorization"] = "Bearer $Token"
$rawJson = Get-Content -LiteralPath $JsonPath -Raw
$uri = "$baseUrl/api/ea/clubs/$EaClubId/friendlies/import?platform=$([uri]::EscapeDataString($Platform))"

Write-Host "Importing EA matches JSON..."
Write-Host "API: $baseUrl"
Write-Host "EA club ID: $EaClubId"
Write-Host "File: $JsonPath"

$result = Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -ContentType "application/json" -Body $rawJson
$result | ConvertTo-Json -Depth 10