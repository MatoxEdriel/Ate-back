using System.Globalization;
using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Application.Features.EmployeeOrders.DTOs;
using Ate.Application.Features.MerchantOrders.DTOs;
using Ate.Application.Features.Restaurants;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ate.Infrastructure.Services;

public class MerchantOrderService : IMerchantOrderService, IScopedDependency
{
    /// <summary>
    /// Flujo del restaurante: Pending → Confirmed → Ready → Delivered.
    /// Puede cancelar mientras no lo haya preparado (Pending o Confirmed).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions = new Dictionary<string, string[]>
    {
        [EmployeeOrderStatus.Confirmed] = [EmployeeOrderStatus.Pending],
        [EmployeeOrderStatus.Ready] = [EmployeeOrderStatus.Pending, EmployeeOrderStatus.Confirmed],
        [EmployeeOrderStatus.Delivered] = [EmployeeOrderStatus.Ready],
        [EmployeeOrderStatus.Cancelled] = [EmployeeOrderStatus.Pending, EmployeeOrderStatus.Confirmed]
    };

    private static readonly IReadOnlyDictionary<string, string> StatusNotes = new Dictionary<string, string>
    {
        [EmployeeOrderStatus.Confirmed] = "Pedido confirmado por el restaurante.",
        [EmployeeOrderStatus.Ready] = "Pedido listo para retirar o enviar.",
        [EmployeeOrderStatus.Delivered] = "Pedido entregado.",
        [EmployeeOrderStatus.Cancelled] = "Pedido cancelado por el restaurante."
    };

    private readonly IApplicationDbContext _context;
    private readonly IMongoCollection<EmployeeOrder> _orders;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MerchantOrderService(
        IApplicationDbContext context,
        IMongoDbContext mongoContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _orders = mongoContext.GetCollection<EmployeeOrder>(MongoCollections.EmployeeOrders);
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IReadOnlyList<MerchantOrderDto>>> GetOrdersAsync(Guid merchantUserId, string? serviceDate, CancellationToken cancellationToken = default)
    {
        var date = string.IsNullOrWhiteSpace(serviceDate)
            ? DateOnly.FromDateTime(_dateTimeProvider.BusinessNow.DateTime)
            : DateOnly.TryParseExact(serviceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed
                : (DateOnly?)null;

        if (date == null)
        {
            return Result.Failure<IReadOnlyList<MerchantOrderDto>>(MerchantOrderErrors.InvalidDate);
        }

        var restaurantId = await GetRestaurantIdAsync(merchantUserId, cancellationToken);
        if (!restaurantId.HasValue)
        {
            return Result.Failure<IReadOnlyList<MerchantOrderDto>>(MerchantOrderErrors.RestaurantNotFound);
        }

        var dateText = date.Value.ToString("yyyy-MM-dd");
        var orders = await _orders
            .Find(o => o.RestaurantId == restaurantId.Value && o.ServiceDate == dateText)
            .SortBy(o => o.OrderedAt)
            .ToListAsync(cancellationToken);

        var companyNames = await GetCompanyNamesAsync(orders.Select(o => o.CompanyId), cancellationToken);
        IReadOnlyList<MerchantOrderDto> result = orders.Select(o => ToDto(o, companyNames)).ToList();
        return Result.Success(result);
    }

    public async Task<Result<MerchantOrderDto>> UpdateStatusAsync(Guid merchantUserId, string orderId, UpdateMerchantOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var target = AllowedTransitions.Keys.FirstOrDefault(s => string.Equals(s, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            return Result.Failure<MerchantOrderDto>(MerchantOrderErrors.InvalidStatus);
        }

        if (!ObjectId.TryParse(orderId, out _))
        {
            return Result.Failure<MerchantOrderDto>(EmployeeOrderErrors.NotFound);
        }

        var restaurantId = await GetRestaurantIdAsync(merchantUserId, cancellationToken);
        if (!restaurantId.HasValue)
        {
            return Result.Failure<MerchantOrderDto>(MerchantOrderErrors.RestaurantNotFound);
        }

        var exists = await _orders
            .Find(o => o.Id == orderId && o.RestaurantId == restaurantId.Value)
            .AnyAsync(cancellationToken);

        if (!exists)
        {
            return Result.Failure<MerchantOrderDto>(EmployeeOrderErrors.NotFound);
        }

        var now = _dateTimeProvider.UtcNow;
        var change = new EmployeeOrderStatusChange
        {
            Status = target,
            ChangedAt = now,
            ChangedByUserId = merchantUserId,
            Note = StatusNotes[target]
        };

        // The status filter makes the transition atomic: two devices cannot move the same order twice.
        var filter = Builders<EmployeeOrder>.Filter.And(
            Builders<EmployeeOrder>.Filter.Eq(o => o.Id, orderId),
            Builders<EmployeeOrder>.Filter.Eq(o => o.RestaurantId, restaurantId.Value),
            Builders<EmployeeOrder>.Filter.In(o => o.Status, AllowedTransitions[target]));

        var update = Builders<EmployeeOrder>.Update
            .Set(o => o.Status, target)
            .Set(o => o.UpdatedAt, now)
            .Push(o => o.StatusHistory, change);

        // A closed order (delivered or cancelled) frees the slot: the employee can order that meal again.
        if (target is EmployeeOrderStatus.Delivered or EmployeeOrderStatus.Cancelled)
        {
            update = update.Unset(o => o.ActiveSlot);
        }

        var updated = await _orders.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<EmployeeOrder> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (updated == null)
        {
            return Result.Failure<MerchantOrderDto>(MerchantOrderErrors.InvalidTransition);
        }

        var companyNames = await GetCompanyNamesAsync([updated.CompanyId], cancellationToken);
        return Result.Success(ToDto(updated, companyNames));
    }

    private async Task<Guid?> GetRestaurantIdAsync(Guid merchantUserId, CancellationToken cancellationToken)
    {
        return await _context.Restaurants
            .AsNoTracking()
            .Where(r => r.MerchantUserId == merchantUserId && r.IsActive)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> GetCompanyNamesAsync(IEnumerable<Guid> companyIds, CancellationToken cancellationToken)
    {
        var ids = companyIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.Companies
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => string.IsNullOrWhiteSpace(c.TradeName) ? c.BusinessName : c.TradeName, cancellationToken);
    }

    private static MerchantOrderDto ToDto(EmployeeOrder order, IReadOnlyDictionary<Guid, string> companyNames)
    {
        return new MerchantOrderDto(
            order.Id,
            order.TicketNumber,
            order.EmployeeName,
            order.EmployeeDocument,
            order.EmployeeCode,
            order.Department,
            companyNames.GetValueOrDefault(order.CompanyId, string.Empty),
            MealTypeCodes.ToCode(order.MealType),
            order.ServiceDate,
            order.Items
                .Select(i => new EmployeeOrderItemDto(i.DishId, i.Name, i.Description, i.Protein, i.Quantity, i.UnitPrice, i.Subtotal, i.WithRice, i.Drink, i.Notes))
                .ToList(),
            order.TotalAmount,
            order.Currency,
            order.Status,
            order.DeliveryMode,
            order.DeliveryAddress,
            order.Notes,
            order.OrderedAt,
            order.UpdatedAt);
    }
}
