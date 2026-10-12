# RadWareF1 API

## Recommended cloud database

This API is now prepared to use **Supabase PostgreSQL** as the default hosted database option.

The code now supports both:

- a regular Npgsql connection string in `ConnectionStrings:Default`
- a Postgres URL such as `postgres://user:password@host/db?sslmode=require`
- environment variables commonly used in hosted setups:
  - `SUPABASE_DB_URL`
  - `SUPABASE_DIRECT_URL`
  - `DATABASE_URL`
  - `ConnectionStrings__Default`

## Why Supabase fits this project

- this backend already uses PostgreSQL
- no ORM/provider rewrite is needed
- no schema redesign is needed
- Supabase connection details plug directly into the new config flow
- you keep the current .NET API and auth while moving data to the cloud

## Local setup with user secrets

From the `RadWareF1.API` folder:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=db.your-project-ref.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your_password;Ssl Mode=Require"
```

## Local setup with a Supabase URL

If you want to use the URL copied from Supabase instead of a classic Npgsql connection string:

```bash
export SUPABASE_DB_URL="postgres://postgres:your_password@db.your-project-ref.supabase.co:5432/postgres?sslmode=require"
dotnet run --project "/Users/radoslawkrowicki/RiderProjects/RadWareF1/RadWareF1.API/RadWareF1.API.csproj" --launch-profile http
```

## Which Supabase connection should you use?

Prefer the regular PostgreSQL connection details from the Supabase dashboard.

- If Supabase gives you a classic connection string, put it in `ConnectionStrings:Default`.
- If Supabase gives you a URL, put it in `SUPABASE_DB_URL`.
- If you later see migration or tooling issues with one endpoint, switch to the direct database connection details from the dashboard and place them in `SUPABASE_DIRECT_URL` or `ConnectionStrings:Default`.

## Applying migrations

If this is a brand-new hosted database:

```bash
dotnet ef database update --project "/Users/radoslawkrowicki/RiderProjects/RadWareF1/RadWareF1.Persistance/RadWareF1.Persistance.csproj" --startup-project "/Users/radoslawkrowicki/RiderProjects/RadWareF1/RadWareF1.API/RadWareF1.API.csproj"
```

## Quick migration checklist

1. Create the Supabase project.
2. Copy the database password / connection string from the Supabase dashboard.
3. Save it in user secrets or environment variables.
4. Run `dotnet ef database update`.
5. Start the API and verify `/swagger` opens correctly.

