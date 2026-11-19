using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Application.Features.RestaurantBadges;
using Ate.Application.Features.RestaurantBadges.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Ate.Infrastructure.Services;

public class RestaurantBadgeService : IRestaurantBadgeService, IScopedDependency
{
    private const int SummaryWindowDays = 90;

    private static readonly SemaphoreSlim RecalculationLock = new(1, 1);

    private readonly IApplicationDbContext _context;
    private readonly IMongoCollection<EmployeeOrder> _orders;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestaurantBadgeService(IApplicationDbContext context, IMongoDbContext mongoContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _orders = mongoContext.GetCollection<EmployeeOrder>(MongoCollections.EmployeeOrders);
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task RecalculateAsync(Guid restaurantId, CancellationToken cancellationToken = default)
    {
        await RecalculationLock.WaitAsync(cancellationToken);
        try
        {
            var definitions = await ActiveDefinitionsAsync(cancellationToken);
            var ratings = await RatingsAsync(restaurantId, since: null, cancellationToken);
            var existing = await _context.RestaurantBadges
                .Where(rb => rb.RestaurantId == restaurantId)
                .ToDictionaryAsync(rb => rb.BadgeCode, cancellationToken);
            var now = _dateTimeProvider.UtcNow;

            foreach (var definition in definitions)
            {
                var row = existing.GetValueOrDefault(definition.Code);
                var current = row == null
                    ? new BadgeState(false, null, null)
                    : new BadgeState(row.IsHeld, row.EarnedAt, row.LostAt);
                var next = RestaurantBadgeCalculator.Next(definition, current, AnswersOf(ratings, definition.Category), now);

                if (next == current)
                {
                    continue;
                }

                if (row == null)
                {
                    row = new RestaurantBadge { RestaurantId = restaurantId, BadgeCode = definition.Code };
                    _context.RestaurantBadges.Add(row);
                }

                row.IsHeld = next.IsHeld;
                row.EarnedAt = next.EarnedAt;
                row.LostAt = next.LostAt;
                row.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            RecalculationLock.Release();
        }
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<RestaurantBadgeDto>>> GetHeldAsync(
        IReadOnlyCollection<Guid> restaurantIds, CancellationToken cancellationToken = default)
    {
        if (restaurantIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<RestaurantBadgeDto>>();
        }

        var held = await _context.RestaurantBadges
            .AsNoTracking()
            .Where(rb => restaurantIds.Contains(rb.RestaurantId) && rb.IsHeld && rb.Badge.IsActive)
            .OrderBy(rb => rb.Badge.SortOrder)
            .Select(rb => new
            {
                rb.RestaurantId,
                Badge = new RestaurantBadgeDto(rb.Badge.Code, rb.Badge.Name, rb.Badge.Description, rb.Badge.Icon)
            })
            .ToListAsync(cancellationToken);

        return held
            .GroupBy(h => h.RestaurantId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<RestaurantBadgeDto>)g.Select(h => h.Badge).ToList());
    }

    public async Task<Result<IReadOnlyList<MerchantBadgeSummaryDto>>> GetMerchantSummaryAsync(
        Guid merchantUserId, CancellationToken cancellationToken = default)
    {
        var restaurantId = await _context.Restaurants
            .AsNoTracking()
            .Where(r => r.MerchantUserId == merchantUserId)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (restaurantId == null)
        {
            return Result.Failure<IReadOnlyList<MerchantBadgeSummaryDto>>(MerchantOrderErrors.RestaurantNotFound);
        }

        var definitions = await ActiveDefinitionsAsync(cancellationToken);
        var states = await _context.RestaurantBadges
            .AsNoTracking()
            .Where(rb => rb.RestaurantId == restaurantId.Value)
            .ToDictionaryAsync(rb => rb.BadgeCode, cancellationToken);
        var ratings = await RatingsAsync(restaurantId.Value, _dateTimeProvider.UtcNow.AddDays(-SummaryWindowDays), cancellationToken);

        IReadOnlyList<MerchantBadgeSummaryDto> summary = definitions
            .Select(d =>
            {
                var state = states.GetValueOrDefault(d.Code);
                var answers = AnswersOf(ratings, d.Category);
                var isHeld = state?.IsHeld == true;
                return new MerchantBadgeSummaryDto(
                    d.Code,
                    d.Name,
                    d.Description,
                    d.Icon,
                    isHeld,
                    isHeld ? state!.EarnedAt : null,
                    answers.Count(a => a.Positive),
                    answers.Count(a => !a.Positive));
            })
            .ToList();

        return Result.Success(summary);
    }

    private Task<List<RestaurantBadgeDefinition>> ActiveDefinitionsAsync(CancellationToken cancellationToken) =>
        _context.RestaurantBadgeDefinitions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(cancellationToken);

    private async Task<List<EmployeeOrderRating>> RatingsAsync(Guid restaurantId, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        var filter = Builders<EmployeeOrder>.Filter;
        var query = filter.Eq(o => o.RestaurantId, restaurantId)
            & filter.Exists(o => o.Rating)
            & filter.Eq(o => o.Rating!.Skipped, false);
        if (since.HasValue)
        {
            query &= filter.Gte(o => o.Rating!.RatedAt, since.Value);
        }

        return await _orders.Find(query)
            .Project(o => o.Rating!)
            .ToListAsync(cancellationToken);
    }

    private static List<BadgeAnswer> AnswersOf(IEnumerable<EmployeeOrderRating> ratings, string category) =>
        ratings
            .Where(r => r.Answers.ContainsKey(category))
            .Select(r => new BadgeAnswer(r.Answers[category], r.RatedAt))
            .ToList();
}
