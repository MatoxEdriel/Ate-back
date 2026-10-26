using Ate.Application.Common.Interfaces;
using Ate.Infrastructure.Persistence;
using Npgsql;

namespace Ate.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        var context = scope.ServiceProvider.GetRequiredService<AteDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        try
        {
            await DatabaseSchemaInitializer.ApplySchemaAsync(dataSource);
            logger.LogInformation("Database schema synchronized from 01_init_schema.sql.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying the database schema script.");
            return;
        }

        try
        {
            await AteDbSeeder.SeedAsync(context, passwordHasher);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
