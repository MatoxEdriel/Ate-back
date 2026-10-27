using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Entities;
using Ate.Infrastructure.Persistence.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
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

        var connectionString = configuration.GetConnectionString(ConfigurationConstants.ConnectionStrings.MongoDbConnection);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = options.ConnectionString;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        var databaseName = string.IsNullOrWhiteSpace(options.DatabaseName)
            ? "ate_nosql_db"
            : options.DatabaseName;

        RegisterMappings();

        var mongoClient = new MongoClient(connectionString);

        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
        services.AddScoped<IMongoDbContext>(sp => new MongoDbContext(sp.GetRequiredService<IMongoClient>(), databaseName));

        return services;
    }

    private static void RegisterMappings()
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.TryRegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
        BsonSerializer.TryRegisterSerializer(new DateTimeOffsetSerializer(BsonType.DateTime));

        ConventionRegistry.Register(
            "AteConventions",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new IgnoreIfNullConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            },
            type => type.Namespace?.StartsWith("Ate.") == true);

        BsonClassMap.TryRegisterClassMap<EmployeeOrder>(map =>
        {
            map.AutoMap();
            map.MapIdMember(o => o.Id)
                .SetIdGenerator(StringObjectIdGenerator.Instance)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));
        });

        BsonClassMap.TryRegisterClassMap<DishCatalogEntry>(map =>
        {
            map.AutoMap();
            map.MapIdMember(e => e.Id);
        });
    }
}
