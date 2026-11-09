using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Achievements.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Ate.Infrastructure.Services;

public class AchievementService : IAchievementService, IScopedDependency
{
    private const string HiddenName = "???";
    private const string HiddenDescription = "Trofeo oculto. Sigue pidiendo para descubrirlo.";

    private static readonly string[] TierOrder =
        [AchievementTiers.Bronze, AchievementTiers.Silver, AchievementTiers.Gold, AchievementTiers.Platinum];

    /// <summary>
    /// The worker and a GET may evaluate the same user at once; evaluations are short, so one at a time
    /// avoids duplicate inserts. Enough for a single API instance; with several, use a DB upsert instead.
    /// </summary>
    private static readonly SemaphoreSlim EvaluationLock = new(1, 1);

    private readonly IApplicationDbContext _context;
    private readonly IMongoCollection<EmployeeOrder> _orders;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AchievementService(IApplicationDbContext context, IMongoDbContext mongoContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _orders = mongoContext.GetCollection<EmployeeOrder>(MongoCollections.EmployeeOrders);
        _dateTimeProvider = dateTimeProvider;
    }

    /// <remarks>
    /// Platinum is evaluated last because it depends on every other trophy. Unlocked trophies keep
    /// their best progress, so a broken streak never takes one away.
    /// </remarks>
    public async Task<int> EvaluateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await EvaluationLock.WaitAsync(cancellationToken);
        try
        {
            var definitions = await ActiveDefinitionsAsync(cancellationToken);
            var orders = await _orders.Find(o => o.UserId == userId).ToListAsync(cancellationToken);
            var metrics = AchievementMetricCalculator.Compute(orders);

            var existing = await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .ToDictionaryAsync(ua => ua.AchievementCode, cancellationToken);

            var now = _dateTimeProvider.UtcNow;
            var newlyUnlocked = 0;

            foreach (var definition in definitions.OrderBy(d => d.Metric == AchievementMetrics.AllTrophies))
            {
                var target = TargetOf(definition, definitions);
                var progress = definition.Metric == AchievementMetrics.AllTrophies
                    ? definitions.Count(d => d.Metric != AchievementMetrics.AllTrophies && existing.GetValueOrDefault(d.Code)?.UnlockedAt != null)
                    : metrics.GetValueOrDefault(definition.Metric);

                if (!existing.TryGetValue(definition.Code, out var row))
                {
                    if (progress == 0)
                    {
                        continue;
                    }

                    row = new UserAchievement { UserId = userId, AchievementCode = definition.Code };
                    _context.UserAchievements.Add(row);
                    existing[definition.Code] = row;
                }

                var shownProgress = row.UnlockedAt != null ? Math.Max(row.Progress, progress) : progress;
                if (row.Progress != shownProgress)
                {
                    row.Progress = shownProgress;
                    row.UpdatedAt = now;
                }

                if (row.UnlockedAt == null && progress >= target)
                {
                    row.UnlockedAt = now;
                    row.UpdatedAt = now;
                    newlyUnlocked++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return newlyUnlocked;
        }
        finally
        {
            EvaluationLock.Release();
        }
    }

    /// <remarks>Evaluating on read keeps the room fresh and makes new trophies retroactive for this user.</remarks>
    public async Task<Result<AchievementsOverviewDto>> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await EvaluateAsync(userId, cancellationToken);

        var definitions = await ActiveDefinitionsAsync(cancellationToken);
        var rows = await UserRowsAsync(userId, cancellationToken);
        var achievements = definitions.Select(d => ToDto(d, rows.GetValueOrDefault(d.Code), definitions)).ToList();

        var unlocked = achievements.Count(a => a.UnlockedAt != null);
        var total = achievements.Count;
        var tiers = TierOrder
            .Select(tier => new AchievementTierSummaryDto(
                tier,
                achievements.Count(a => a.Tier == tier && a.UnlockedAt != null),
                achievements.Count(a => a.Tier == tier)))
            .Where(t => t.Total > 0)
            .ToList();

        var percent = total == 0 ? 0 : (int)Math.Floor(unlocked * 100.0 / total);
        return Result.Success(new AchievementsOverviewDto(unlocked, total, percent, tiers, achievements));
    }

    public async Task<Result<IReadOnlyList<AchievementDto>>> GetUnseenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var definitions = await ActiveDefinitionsAsync(cancellationToken);
        var unseen = await _context.UserAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.UnlockedAt != null && ua.SeenAt == null)
            .OrderBy(ua => ua.UnlockedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<AchievementDto> result = unseen
            .Select(row => (row, definition: definitions.FirstOrDefault(d => d.Code == row.AchievementCode)))
            .Where(x => x.definition != null)
            .Select(x => ToDto(x.definition!, x.row, definitions))
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<int>> MarkSeenAsync(Guid userId, MarkAchievementsSeenRequest request, CancellationToken cancellationToken = default)
    {
        var codes = (request.Codes ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        if (codes.Count == 0)
        {
            return Result.Success(0);
        }

        var rows = await _context.UserAchievements
            .Where(ua => ua.UserId == userId && codes.Contains(ua.AchievementCode) && ua.UnlockedAt != null && ua.SeenAt == null)
            .ToListAsync(cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        foreach (var row in rows)
        {
            row.SeenAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(rows.Count);
    }

    private Task<List<AchievementDefinition>> ActiveDefinitionsAsync(CancellationToken cancellationToken) =>
        _context.AchievementDefinitions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(cancellationToken);

    private Task<Dictionary<string, UserAchievement>> UserRowsAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.UserAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementCode, cancellationToken);

    /// <summary>Platinum's target is the number of other active trophies; the rest use their threshold.</summary>
    private static int TargetOf(AchievementDefinition definition, IReadOnlyCollection<AchievementDefinition> definitions) =>
        definition.Metric == AchievementMetrics.AllTrophies
            ? Math.Max(1, definitions.Count(d => d.Metric != AchievementMetrics.AllTrophies))
            : definition.Threshold;

    private static AchievementDto ToDto(AchievementDefinition definition, UserAchievement? row, IReadOnlyCollection<AchievementDefinition> definitions)
    {
        var target = TargetOf(definition, definitions);
        var unlocked = row?.UnlockedAt != null;
        var concealed = definition.IsHidden && !unlocked;

        return new AchievementDto(
            definition.Code,
            concealed ? HiddenName : definition.Name,
            concealed ? HiddenDescription : definition.Description,
            definition.Tier,
            definition.Icon,
            definition.IsHidden,
            Math.Min(row?.Progress ?? 0, target),
            target,
            row?.UnlockedAt);
    }
}
