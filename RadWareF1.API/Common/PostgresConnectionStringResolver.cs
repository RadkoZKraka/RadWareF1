using Npgsql;

namespace RadWareF1.Common;

public static class PostgresConnectionStringResolver
{
    private const string DefaultConnectionName = "Default";
    private const string TestingFallbackConnectionString = "Host=localhost;Port=5432;Database=radwaref1_tests;Username=test;Password=test";
    private static readonly string[] EnvironmentVariableNames =
    [
        "ConnectionStrings__Default",
        "SUPABASE_DB_URL",
        "SUPABASE_DIRECT_URL",
        "DATABASE_URL"
    ];

    public static string Resolve(IConfiguration configuration, string? environmentName = null)
    {
        var configuredConnectionString = configuration.GetConnectionString(DefaultConnectionName);
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        var environmentValue = ResolveFromEnvironment(configuration);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return TryConvertToConnectionString(environmentValue);
        }

        if (string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return TestingFallbackConnectionString;
        }

        throw new InvalidOperationException(
            "PostgreSQL connection is not configured. Set ConnectionStrings:Default, ConnectionStrings__Default, SUPABASE_DB_URL, SUPABASE_DIRECT_URL, or DATABASE_URL.");
    }

    private static string? ResolveFromEnvironment(IConfiguration configuration)
    {
        foreach (var variableName in EnvironmentVariableNames)
        {
            var value = configuration[variableName]
                ?? Environment.GetEnvironmentVariable(variableName);

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string TryConvertToConnectionString(string value)
    {
        return LooksLikeDatabaseUrl(value)
            ? FromDatabaseUrl(value)
            : value;
    }

    private static bool LooksLikeDatabaseUrl(string value)
    {
        return value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
    }

    public static string FromDatabaseUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("DATABASE_URL must be a valid absolute URI.");
        }

        if (!string.Equals(uri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DATABASE_URL must use the postgres:// or postgresql:// scheme.");
        }

        var userInfoParts = uri.UserInfo.Split(':', 2);
        var username = userInfoParts.Length > 0 ? Uri.UnescapeDataString(userInfoParts[0]) : string.Empty;
        var password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty;
        var databaseName = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("DATABASE_URL must include a username.");
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("DATABASE_URL must include a database name.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = username,
            Password = password,
            Database = databaseName,
            SslMode = SslMode.Require
        };

        ApplySupportedQueryStringOptions(builder, uri.Query);

        return builder.ConnectionString;
    }

    private static void ApplySupportedQueryStringOptions(NpgsqlConnectionStringBuilder builder, string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return;
        }

        var pairs = queryString.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(keyValue[0]).Trim();
            var value = keyValue.Length > 1 ? Uri.UnescapeDataString(keyValue[1]).Trim() : string.Empty;

            switch (key.ToLowerInvariant())
            {
                case "sslmode":
                    if (Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode))
                    {
                        builder.SslMode = sslMode;
                    }
                    break;

                case "pooling":
                    if (bool.TryParse(value, out var pooling))
                    {
                        builder.Pooling = pooling;
                    }
                    break;

                case "minimum pool size":
                case "min pool size":
                case "minpoolsize":
                    if (int.TryParse(value, out var minPoolSize))
                    {
                        builder.MinPoolSize = minPoolSize;
                    }
                    break;

                case "maximum pool size":
                case "max pool size":
                case "maxpoolsize":
                    if (int.TryParse(value, out var maxPoolSize))
                    {
                        builder.MaxPoolSize = maxPoolSize;
                    }
                    break;
            }
        }
    }
}



