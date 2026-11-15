using Ate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Ate.Infrastructure.Services;

/// <summary>
/// Escribe en security_events con una conexión propia (no el DbContext de la petición), para no guardar
/// cambios pendientes de otra operación por accidente. También deja una línea en el log de categoría Security.
/// </summary>
public sealed class SecurityEventLog : ISecurityEventLog, IScopedDependency
{
    private const int MaxDetailLength = 500;

    private readonly NpgsqlDataSource _dataSource;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger _logger;

    public SecurityEventLog(NpgsqlDataSource dataSource, ICurrentUserService currentUser, ILoggerFactory loggerFactory)
    {
        _dataSource = dataSource;
        _currentUser = currentUser;
        _logger = loggerFactory.CreateLogger("Security");
    }

    public async Task RecordAsync(string eventType, Guid? userId, string outcome, string? detail = null, CancellationToken cancellationToken = default)
    {
        var ip = _currentUser.IpAddress;
        if (detail is { Length: > MaxDetailLength })
        {
            detail = detail[..MaxDetailLength];
        }

        _logger.LogInformation("Security event {EventType} {Outcome} user={UserId} ip={ClientIp} {Detail}", eventType, outcome, userId, ip, detail);

        try
        {
            await using var command = _dataSource.CreateCommand(
                "INSERT INTO security_events (id, occurred_at, event_type, user_id, outcome, ip, detail) VALUES ($1, now(), $2, $3, $4, $5, $6)");
            command.Parameters.Add(new NpgsqlParameter { Value = Guid.CreateVersion7() });
            command.Parameters.Add(new NpgsqlParameter { Value = eventType });
            command.Parameters.Add(new NpgsqlParameter { Value = (object?)userId ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter { Value = outcome });
            command.Parameters.Add(new NpgsqlParameter { Value = (object?)ip ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter { Value = (object?)detail ?? DBNull.Value });
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Security event {EventType} could not be stored.", eventType);
        }
    }
}
