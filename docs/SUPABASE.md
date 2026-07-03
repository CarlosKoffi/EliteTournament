# Supabase Setup

CPElite stores application data in Supabase PostgreSQL.

## Local Configuration

Create `src/CPElite.Api/appsettings.Development.json` and add your Supabase connection string:

```json
{
  "ConnectionStrings": {
    "Supabase": "Host=db.your-project-ref.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SslMode=Require;Trust Server Certificate=true"
  }
}
```

Do not commit `appsettings.Development.json`.

## Environment Variable Alternative

You can also set:

```powershell
$env:ConnectionStrings__Supabase="Host=db.your-project-ref.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SslMode=Require;Trust Server Certificate=true"
```

## Next Step

Before using the live database, add the first EF Core migration and apply it to Supabase.
