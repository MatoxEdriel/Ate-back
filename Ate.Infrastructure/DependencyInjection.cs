using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Infrastructure.Extensions;
using Ate.Infrastructure.Persistence;
using Ate.Infrastructure.Persistence.Interceptors;
using Ate.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Ate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString(ConfigurationConstants.ConnectionStrings.DefaultConnection)
            ?? throw new InvalidOperationException($"Connection string '{ConfigurationConstants.ConnectionStrings.DefaultConnection}' was not found.");

        var dataSource = NpgsqlDataSource.Create(connectionString);
        services.AddSingleton(dataSource);

        services.AddScoped<DishCatalogSyncInterceptor>();

        services.AddDbContext<AteDbContext>((sp, options) =>
        {
            options.UseNpgsql(dataSource)
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(sp.GetRequiredService<DishCatalogSyncInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AteDbContext>());

        services.Scan(scan => scan
            .FromAssemblyOf<AteDbContext>()
            .AddClasses(classes => classes.AssignableTo<IScopedDependency>())
                .AsMatchingInterface()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo<ITransientDependency>())
                .AsMatchingInterface()
                .WithTransientLifetime()
            .AddClasses(classes => classes.AssignableTo<ISingletonDependency>())
                .AsMatchingInterface()
                .WithSingletonLifetime()
            .AddClasses(classes => classes.Where(type =>
                type.Name.EndsWith("Service") ||
                type.Name.EndsWith("Repository")))
                .AsMatchingInterface()
                .WithScopedLifetime());

        services.AddMongoDb(configuration);

        return services;
    }
}
