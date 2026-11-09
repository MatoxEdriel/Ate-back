using System.Threading.Channels;
using Ate.Application.Common.Interfaces;

namespace Ate.Infrastructure.Services;

/// <summary>
/// In-memory bounded queue of user ids consumed by <see cref="AchievementWorker"/>.
/// A dropped item is harmless: progress is recomputed from the orders on the next evaluation.
/// </summary>
public sealed class AchievementQueue : IAchievementQueue
{
    private const int Capacity = 1000;

    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(Capacity)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true
    });

    internal ChannelReader<Guid> Reader => _channel.Reader;

    public bool TryEnqueue(Guid userId) => _channel.Writer.TryWrite(userId);
}
