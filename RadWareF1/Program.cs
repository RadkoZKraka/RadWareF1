using Microsoft.EntityFrameworkCore;
using RadWareF1.Persistance;

namespace RadWareF1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = builder.Configuration["Auth:Authority"];
                options.TokenValidationParameters = new()
                {
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Auth:Audience"]
                };
            });

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql("Host=localhost;Port=5432;Database=app-db;Username=postgres;Password=postgres"));

        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}