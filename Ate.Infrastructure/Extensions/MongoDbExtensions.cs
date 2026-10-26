using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Infrastructure.Persistence.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ate.Infrastructure.Extensions;

public static class MongoDbExtensions
{
    public static IServiceCollection AddMongoDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new MongoDbOptions();
        configuration.GetSection(MongoDbOptions.SectionName).Bind(options);

        var connectionString = configuration.GetConnectionString(ConfigurationConstants.ConnectionStrings.MongoDbConnection)
            ?? options.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        var databaseName = string.IsNullOrWhiteSpace(options.DatabaseName)
            ? "ate_nosql_db"
            : options.DatabaseName;

        var mongoClient = new MongoClient(connectionString);

        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
        services.AddScoped<IMongoDbContext>(sp => new MongoDbContext(sp.GetRequiredService<IMongoClient>(), databaseName));

        return services;
    }
}
