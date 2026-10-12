using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RadWareF1.Application.Abstractions;
using RadWareF1.Domain;
using RadWareF1.Persistance;

namespace RadWareF1.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection _connection = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestOrBearer";
                options.DefaultChallengeScheme = "TestOrBearer";
            })
            .AddPolicyScheme("TestOrBearer", "Test or Bearer", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    if (context.Request.Headers.Authorization.Count > 0)
                    {
                        return Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                    }

                    return TestAuthenticationHandler.SchemeName;
                };
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            dbContext.Database.EnsureCreated();

            Seed(dbContext, passwordService);
        });
    }

    private static void Seed(AppDbContext dbContext, IPasswordService passwordService)
    {
        if (dbContext.Users.Any(x => x.Email == "test@test.com"))
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com"
        };

        user.PasswordHash = passwordService.HashPassword(user, "test123");

        dbContext.Users.Add(user);
        dbContext.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}