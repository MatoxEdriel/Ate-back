using Ate.Domain.Entities;

namespace Ate.Application.Common.Interfaces;

public interface IIdempotencyStore
{
    Task<bool> TryReserveAsync(string id, string requestHash, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<bool> TryTakeOverAsync(string id, DateTimeOffset staleBefore, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<IdempotencyRecord?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task CompleteAsync(string id, int statusCode, string body, CancellationToken cancellationToken = default);

    Task ReleaseAsync(string id, CancellationToken cancellationToken = default);
}
