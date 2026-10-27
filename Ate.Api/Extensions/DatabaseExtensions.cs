using Ate.Application.Common.Interfaces;
using Ate.Infrastructure.Persistence;
using Ate.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;
using Npgsql;

namespace Ate.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        await InitialiseMongoDbAsync(app);

        if (app.Environment.IsDevelopment())
        {
            await InitialisePostgresAsync(app);
        }

        await SyncDishCatalogAsync(app);
    }

    private static async Task InitialisePostgresAsync(WebApplication app)
    {
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

    private static async Task InitialiseMongoDbAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var mongoDatabase = scope.ServiceProvider.GetService<IMongoDatabase>();

        if (mongoDatabase == null)
        {
            logger.LogWarning("MongoDB is not configured; skipping collection initialization.");
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await MongoDbInitializer.EnsureCollectionsAsync(mongoDatabase, timeout.Token);
            logger.LogInformation("MongoDB collections and indexes ensured.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing MongoDB collections.");
        }
    }

    /// <summary>
    /// Copia completa de restaurant_dishes a dish_catalog. Corrige cualquier cambio que el interceptor
    /// no haya podido replicar (por ejemplo, si Mongo estaba caído).
    /// </summary>
    private static async Task SyncDishCatalogAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        if (scope.ServiceProvider.GetService<IMongoDbContext>() == null)
        {
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var syncService = scope.ServiceProvider.GetRequiredService<IDishCatalogSyncService>();
            var count = await syncService.SyncAllAsync(timeout.Token);
            logger.LogInformation("MongoDB dish catalog synchronized ({Count} dishes).", count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while synchronizing the MongoDB dish catalog.");
        }
    }
}
