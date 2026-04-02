using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RadWareF1.Application.Abstractions;
using RadWareF1.Application.Contracts.Auth;
using RadWareF1.Application.Services;
using RadWareF1.Persistance;

namespace RadWareF1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddUserSecrets<Program>(optional: true);

        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        Console.WriteLine("=== OnMessageReceived ===");
                        Console.WriteLine($"Path: {context.Request.Path}");
                        Console.WriteLine($"Authorization: {context.Request.Headers.Authorization}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("=== OnTokenValidated ===");
                        Console.WriteLine($"Path: {context.Request.Path}");
                        Console.WriteLine("Token validated");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("=== OnAuthenticationFailed ===");
                        Console.WriteLine($"Path: {context.Request.Path}");
                        Console.WriteLine(context.Exception.ToString());
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine("=== OnChallenge ===");
                        Console.WriteLine($"Path: {context.Request.Path}");
                        Console.WriteLine($"Error: {context.Error}");
                        Console.WriteLine($"Description: {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddScoped<IPasswordService, PasswordService>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();

        builder.Services.AddSwaggerGen();

        builder.Services.AddEndpointsApiExplorer();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}