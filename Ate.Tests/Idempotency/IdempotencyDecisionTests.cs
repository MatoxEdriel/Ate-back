using Ate.Application.Common.Idempotency;
using Ate.Domain.Entities;

namespace Ate.Tests.Idempotency;

public class IdempotencyDecisionTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(60);

    private static IdempotencyRecord Record(bool completed, string hash = "h", int secondsAgo = 1) => new()
    {
        CreatedAt = Now.AddSeconds(-secondsAgo),
        Id = "user:key",
        RequestHash = hash,
        Completed = completed,
        StatusCode = completed ? 200 : null,
        Body = completed ? "{}" : null,
    };

    [Fact]
    public void Execute_WhenNoRecord() =>
        Assert.Equal(IdempotencyOutcome.Execute, IdempotencyDecision.Decide(null, "h", Now, Lease));

    [Fact]
    public void Replay_WhenCompletedWithSameHash() =>
        Assert.Equal(IdempotencyOutcome.Replay, IdempotencyDecision.Decide(Record(completed: true), "h", Now, Lease));

    [Fact]
    public void InProgress_WhenNotCompletedWithSameHash() =>
        Assert.Equal(IdempotencyOutcome.InProgress, IdempotencyDecision.Decide(Record(completed: false), "h", Now, Lease));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KeyReused_WhenHashDiffers(bool completed) =>
        Assert.Equal(IdempotencyOutcome.KeyReused, IdempotencyDecision.Decide(Record(completed), "otro", Now, Lease));

    [Fact]
    public void Execute_WhenInProgressTicketIsOlderThanTheLease() =>
        Assert.Equal(IdempotencyOutcome.Execute, IdempotencyDecision.Decide(Record(completed: false, secondsAgo: 61), "h", Now, Lease));

    [Fact]
    public void Replay_EvenWhenCompletedTicketIsOld() =>
        Assert.Equal(IdempotencyOutcome.Replay, IdempotencyDecision.Decide(Record(completed: true, secondsAgo: 3600), "h", Now, Lease));
}
