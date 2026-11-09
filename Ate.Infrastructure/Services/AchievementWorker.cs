using Ate.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ate.Infrastructure.Services;

/// <summary>Evaluates the trophies of the users queued in <see cref="AchievementQueue"/>, one scope per user.</summary>
public sealed class AchievementWorker : BackgroundService
{
    private readonly AchievementQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AchievementWorker> _logger;

    public AchievementWorker(AchievementQueue queue, IServiceScopeFactory scopeFactory, ILogger<AchievementWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var userId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var achievements = scope.ServiceProvider.GetRequiredService<IAchievementService>();
                var unlocked = await achievements.EvaluateAsync(userId, stoppingToken);
                if (unlocked > 0)
                {
                    _logger.LogInformation("User {UserId} unlocked {Count} trophies.", userId, unlocked);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trophies of user {UserId} could not be evaluated.", userId);
            }
        }
    }
}
