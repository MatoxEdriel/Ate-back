namespace Ate.Application.Common.Interfaces;

/// <summary>Asks for a background re-evaluation of a user's trophies without slowing the caller.</summary>
public interface IAchievementQueue
{
    bool TryEnqueue(Guid userId);
}
