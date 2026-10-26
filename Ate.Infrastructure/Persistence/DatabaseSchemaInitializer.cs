using Npgsql;

namespace Ate.Infrastructure.Persistence;

public static class DatabaseSchemaInitializer
{
    private const string SchemaResourceName = "Ate.Database.InitSchema.sql";

    public static async Task ApplySchemaAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken = default)
    {
        var assembly = typeof(DatabaseSchemaInitializer).Assembly;
        await using var stream = assembly.GetManifestResourceStream(SchemaResourceName)
            ?? throw new InvalidOperationException($"Embedded schema script '{SchemaResourceName}' was not found.");

        using var reader = new StreamReader(stream);
        var script = await reader.ReadToEndAsync(cancellationToken);

        await using var command = dataSource.CreateCommand(script);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
