using Ate.Application.Common.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(StorageOptions.SectionName);
        services.AddOptions<StorageOptions>()
            .Bind(section)
            .Validate(o => !string.IsNullOrWhiteSpace(o.PublicBaseUrl), "Storage:PublicBaseUrl is required.")
            .ValidateOnStart();

        var provider = section.GetValue<string>(nameof(StorageOptions.Provider)) ?? StorageProviders.Local;
        switch (provider)
        {
            case StorageProviders.Local:
                services.AddSingleton<IFileStorage, LocalFileStorage>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Storage:Provider '{provider}' has no adapter yet. Available: {StorageProviders.Local}.");
        }

        return services;
    }
}
