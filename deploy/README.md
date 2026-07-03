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
