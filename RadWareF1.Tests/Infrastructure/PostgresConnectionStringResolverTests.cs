using Microsoft.Extensions.Configuration;
using Npgsql;
using RadWareF1.Common;

namespace RadWareF1.Tests.Infrastructure;

public class PostgresConnectionStringResolverTests
{
    [Fact]
    public void Resolve_Should_Return_ConnectionStringsDefault_When_Present()
    {
        var expected = "Host=localhost;Port=5432;Database=radwaref1;Username=postgres;Password=postgres";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = expected
            })
            .Build();

        var result = PostgresConnectionStringResolver.Resolve(configuration);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_Should_Convert_DatabaseUrl_When_ConnectionString_Is_Missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SUPABASE_DB_URL"] = "postgres://test_user:test_password@db.example.com:5432/radwaref1?sslmode=require&pooling=true"
            })
            .Build();

        var result = PostgresConnectionStringResolver.Resolve(configuration);
        var builder = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("radwaref1", builder.Database);
        Assert.Equal("test_user", builder.Username);
        Assert.Equal("test_password", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.True(builder.Pooling);
    }

    [Fact]
    public void Resolve_Should_Return_Raw_Environment_Connection_String_When_Supplied()
    {
        var expected = "Host=db.example.com;Port=5432;Database=postgres;Username=postgres;Password=secret;Ssl Mode=Require";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SUPABASE_DIRECT_URL"] = expected
            })
            .Build();

        var result = PostgresConnectionStringResolver.Resolve(configuration);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_Should_Return_Testing_Fallback_When_Environment_Is_Testing_And_No_Config_Is_Set()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = PostgresConnectionStringResolver.Resolve(configuration, "Testing");
        var builder = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("localhost", builder.Host);
        Assert.Equal("radwaref1_tests", builder.Database);
        Assert.Equal("test", builder.Username);
    }

    [Fact]
    public void FromDatabaseUrl_Should_Decode_Encoded_Credentials()
    {
        var result = PostgresConnectionStringResolver.FromDatabaseUrl(
            "postgres://user%40tenant:pass%3Aword@db.example.com/app-db?sslmode=require");

        var builder = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("user@tenant", builder.Username);
        Assert.Equal("pass:word", builder.Password);
        Assert.Equal("app-db", builder.Database);
    }
}


