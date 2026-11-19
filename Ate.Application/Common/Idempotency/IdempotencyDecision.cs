using Ate.Domain.Entities;

namespace Ate.Application.Common.Idempotency;

public enum IdempotencyOutcome
{
    Execute,
    Replay,
    InProgress,
    KeyReused,
}

public static class IdempotencyDecision
{
    public static IdempotencyOutcome Decide(IdempotencyRecord? existing, string requestHash, DateTimeOffset now, TimeSpan lease)
    {
        if (existing == null)
        {
            return IdempotencyOutcome.Execute;
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return IdempotencyOutcome.KeyReused;
        }

        if (existing.Completed)
        {
            return IdempotencyOutcome.Replay;
        }

        return now - existing.CreatedAt > lease ? IdempotencyOutcome.Execute : IdempotencyOutcome.InProgress;
    }
}
