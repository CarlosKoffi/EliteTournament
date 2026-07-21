# TS Tournament Deployment

## 1. Supabase database

Create a Supabase project, then open the SQL editor and run:

```text
deploy/supabase-initial.sql
```

Use the pooled PostgreSQL connection string for the API host.

For later migrations, run this local helper and enter the Supabase database password when asked:

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\update-supabase-db.ps1
```

The generated SQL for the localized content migration is also available at:

```text
deploy/supabase-localized-content.sql
```

## 2. API environment variables

Set these values in the backend hosting platform:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=Postgres
ConnectionStrings__Supabase=<SUPABASE_POSTGRES_CONNECTION_STRING>
Jwt__Issuer=CPElite
Jwt__Audience=CPElite
Jwt__SigningKey=<LONG_RANDOM_SECRET>
Jwt__ExpiryMinutes=60
EaSync__Enabled=true
EaSync__IntervalMinutes=120
EaMatchVerification__Enabled=true
EaMatchVerification__IntervalMinutes=2
Cors__AllowedOrigins__0=<WEB_APP_URL>
```

Do not commit real connection strings or JWT secrets.

## 3. Web app configuration

Before publishing the Blazor web app, replace the placeholder in:

```text
src/CPElite.Web/wwwroot/appsettings.Production.json
```

with the deployed API URL:

```json
{
  "ApiBaseUrl": "https://your-api-host"
}
```

## 4. Health check

After deployment, verify:

```text
GET https://your-api-host/health
```

Expected response:

```json
{
  "status": "ok",
  "service": "CPElite.Api"
}
```

## 5. Recommended first hosting layout

For the first online version:

- Database: Supabase Postgres.
- Backend: .NET host that supports background services.
- Frontend: static Blazor WebAssembly host.
- Later: Discord bot hosted as a separate worker using the same API.

## 6. Reset production data before launch

To launch with only website labels/texts and no demo/product data, run this script once from the PostgreSQL SQL editor:

```text
deploy/production-content-only-reset.sql
```

It keeps:

- EF Core migration history.
- `LocalizedContents` labels/texts.

It removes:

- users/admin accounts
- teams
- tournaments
- EA snapshots/cache
- billing/slot data

If `Seed__AdminEmail` and `Seed__AdminPassword` are configured, the API will recreate an admin user at startup. Remove those variables if the launch must contain zero users.

## 7. One-shot cleanup during Coolify deployment

If old test accounts still appear as "already registered", the deployed API is still reading users from the production database.

For one deployment only, add one of these variables in Coolify:

```text
StartupCleanup__RegisteredUsers=true
```

This removes registered users and user-owned data, but keeps teams, tournaments and website texts.

For a full public launch reset, use:

```text
StartupCleanup__ContentOnlyReset=true
```

This removes users, teams, tournaments and EA cached data, while keeping `LocalizedContents` and EF migrations.

After the deployment logs show the cleanup completed, remove the variable from Coolify before the next deploy. If the variable stays enabled, the app will clean the same data again on every restart.
## 8. Seed real EA friendly matches for TheSurvivors

After EF migrations have been applied and the TheSurvivors team exists with EA club ID `2148207`, run this SQL once from the Coolify/Supabase PostgreSQL SQL editor:

```text
deploy/seed-thesurvivors-ea-matches.sql
```

The script embeds the paid `matches.json` payload and converts it into real rows in:

- `EaMatchSnapshots`
- `EaFriendlyMatches`
- `EaMatchClubStats`
- `EaMatchPlayerStats`

It is idempotent. If a match with the same EA `matchId` already exists for TheSurvivors, the script deletes the old match/stat rows and recreates them from the latest JSON, so it can be safely rerun after updating the payload.
