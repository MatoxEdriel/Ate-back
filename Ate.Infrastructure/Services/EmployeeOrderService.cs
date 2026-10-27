using System.Security.Cryptography;
using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.EmployeeOrders.DTOs;
using Ate.Application.Features.Restaurants;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ate.Infrastructure.Services;

public class EmployeeOrderService : IEmployeeOrderService, IScopedDependency
{
    private const string TicketChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int TicketSuffixLength = 5;
    private const string WaterDrink = "Agua";
    private const string JuiceDrink = "Jugo";

    /// <summary>Desde esta hora el desayuno que se pide corresponde al día siguiente (igual que el dashboard).</summary>
    private static readonly TimeOnly NextDayBreakfastFrom = new(22, 0);

    private static readonly string[] CancellableStatuses = [EmployeeOrderStatus.Pending];

    private readonly IApplicationDbContext _context;
    private readonly IMongoCollection<EmployeeOrder> _orders;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateEmployeeOrderRequest> _createValidator;

    public EmployeeOrderService(
        IApplicationDbContext context,
        IMongoDbContext mongoContext,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateEmployeeOrderRequest> createValidator)
    {
        _context = context;
        _orders = mongoContext.GetCollection<EmployeeOrder>(MongoCollections.EmployeeOrders);
        _dateTimeProvider = dateTimeProvider;
        _createValidator = createValidator;
    }

    public async Task<Result<EmployeeOrderDto>> CreateAsync(Guid userId, CreateEmployeeOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result.Failure<EmployeeOrderDto>(Error.Validation("EmployeeOrder.InvalidData", "Revisa los datos del pedido.", errors));
        }

        MealTypeCodes.TryParse(request.MealType, out var mealType);

        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Person)
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.Person.UserId == userId && e.IsActive, cancellationToken);

        if (employee == null)
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.EmployeeNotFound);
        }

        var deliveryMode = EmployeeOrderDeliveryModes.Normalize(request.DeliveryMode) ?? EmployeeOrderDeliveryModes.Pickup;
        var isDelivery = deliveryMode == EmployeeOrderDeliveryModes.Delivery;
        var deliveryAddress = isDelivery ? employee.Company.Address?.Trim() : null;
        if (isDelivery && string.IsNullOrEmpty(deliveryAddress))
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.DeliveryAddressMissing);
        }

        var businessNow = _dateTimeProvider.BusinessNow;
        var today = DateOnly.FromDateTime(businessNow.DateTime);
        var currentTime = TimeOnly.FromDateTime(businessNow.DateTime);
        var isNextDayBreakfast = mealType == MealType.Breakfast && currentTime >= NextDayBreakfastFrom;
        var serviceDate = isNextDayBreakfast ? today.AddDays(1) : today;

        var agreement = await _context.CompanyRestaurantAgreements
            .AsNoTracking()
            .Include(a => a.Restaurant)
            .FirstOrDefaultAsync(a => a.CompanyId == employee.CompanyId
                && a.RestaurantId == request.RestaurantId
                && a.Restaurant.IsActive, cancellationToken);

        if (agreement == null || !agreement.IsValidOn(serviceDate))
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.RestaurantNotAvailable);
        }

        var restaurant = agreement.Restaurant;
        var cutoff = restaurant.GetCutoff(mealType);
        if (!isNextDayBreakfast && cutoff.HasValue && currentTime > cutoff.Value)
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.OrderingClosed);
        }

        var dish = await _context.RestaurantDishes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DishId
                && d.RestaurantId == restaurant.Id
                && d.MealType == mealType
                && d.IsAvailable, cancellationToken);

        if (dish == null)
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.DishNotAvailable);
        }

        var drink = string.Equals(request.Drink?.Trim(), EmployeeOrderDrinks.Water, StringComparison.OrdinalIgnoreCase)
            ? WaterDrink
            : JuiceDrink;

        var item = new EmployeeOrderItem
        {
            DishId = dish.Id,
            Name = dish.Name,
            Description = dish.Description,
            Protein = dish.Protein,
            Quantity = 1,
            UnitPrice = dish.Price,
            Subtotal = dish.Price,
            WithRice = dish.HasRice ? request.WithRice ?? true : null,
            Drink = drink
        };

        var now = _dateTimeProvider.UtcNow;
        var serviceDateText = serviceDate.ToString("yyyy-MM-dd");
        var order = new EmployeeOrder
        {
            TicketNumber = GenerateTicketNumber(serviceDate),
            EmployeeId = employee.Id,
            CompanyId = employee.CompanyId,
            UserId = userId,
            EmployeeCode = employee.EmployeeCode ?? string.Empty,
            EmployeeName = $"{employee.Person.FirstName} {employee.Person.LastName}".Trim(),
            EmployeeDocument = employee.Person.DocumentNumber,
            Department = employee.Department,
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            MealType = mealType,
            ServiceDate = serviceDateText,
            ActiveSlot = BuildActiveSlot(employee.Id, serviceDateText, mealType),
            Items = [item],
            TotalAmount = item.Subtotal,
            Status = EmployeeOrderStatus.Pending,
            StatusHistory =
            [
                new EmployeeOrderStatusChange
                {
                    Status = EmployeeOrderStatus.Pending,
                    ChangedAt = now,
                    ChangedByUserId = userId,
                    Note = "Pedido registrado por el colaborador."
                }
            ],
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            DeliveryMode = deliveryMode,
            DeliveryAddress = deliveryAddress,
            OrderedAt = now,
            CreatedAt = now
        };

        try
        {
            await _orders.InsertOneAsync(order, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.AlreadyOrdered);
        }

        return Result.Success(ToDto(order));
    }

    public async Task<Result<PagedResult<EmployeeOrderDto>>> GetMyOrdersAsync(Guid userId, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);
        if (!employeeId.HasValue)
        {
            return Result.Failure<PagedResult<EmployeeOrderDto>>(EmployeeOrderErrors.EmployeeNotFound);
        }

        var filter = Builders<EmployeeOrder>.Filter.Eq(o => o.EmployeeId, employeeId.Value);
        return Result.Success(await GetPagedAsync(filter, pagination, cancellationToken));
    }

    public async Task<Result<EmployeeOrderDto>> GetMyOrderAsync(Guid userId, string orderId, CancellationToken cancellationToken = default)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);
        if (!employeeId.HasValue)
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.EmployeeNotFound);
        }

        if (!ObjectId.TryParse(orderId, out _))
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.NotFound);
        }

        var order = await _orders
            .Find(o => o.Id == orderId && o.EmployeeId == employeeId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return order == null
            ? Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.NotFound)
            : Result.Success(ToDto(order));
    }

    public async Task<Result<EmployeeOrderDto>> CancelMyOrderAsync(Guid userId, string orderId, CancellationToken cancellationToken = default)
    {
        var employeeId = await GetEmployeeIdAsync(userId, cancellationToken);
        if (!employeeId.HasValue)
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.EmployeeNotFound);
        }

        if (!ObjectId.TryParse(orderId, out _))
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.NotFound);
        }

        var order = await _orders
            .Find(o => o.Id == orderId && o.EmployeeId == employeeId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.NotFound);
        }

        if (!CancellableStatuses.Contains(order.Status) || await IsPastCutoffAsync(order, cancellationToken))
        {
            return Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.CannotCancel);
        }

        var now = _dateTimeProvider.UtcNow;
        var change = new EmployeeOrderStatusChange
        {
            Status = EmployeeOrderStatus.Cancelled,
            ChangedAt = now,
            ChangedByUserId = userId,
            Note = "Pedido cancelado por el colaborador."
        };

        var filter = Builders<EmployeeOrder>.Filter.And(
            Builders<EmployeeOrder>.Filter.Eq(o => o.Id, order.Id),
            Builders<EmployeeOrder>.Filter.In(o => o.Status, CancellableStatuses));

        var update = Builders<EmployeeOrder>.Update
            .Set(o => o.Status, EmployeeOrderStatus.Cancelled)
            .Set(o => o.UpdatedAt, now)
            .Unset(o => o.ActiveSlot)
            .Push(o => o.StatusHistory, change);

        var updated = await _orders.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<EmployeeOrder> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        return updated == null
            ? Result.Failure<EmployeeOrderDto>(EmployeeOrderErrors.CannotCancel)
            : Result.Success(ToDto(updated));
    }

    public async Task<Result<PagedResult<EmployeeOrderDto>>> GetCompanyEmployeeOrdersAsync(Guid adminUserId, Guid employeeId, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var companyId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == adminUserId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!companyId.HasValue)
        {
            return Result.Failure<PagedResult<EmployeeOrderDto>>(EmployeeErrors.CompanyNotAssigned);
        }

        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId && e.CompanyId == companyId.Value, cancellationToken);

        if (!employeeExists)
        {
            return Result.Failure<PagedResult<EmployeeOrderDto>>(EmployeeErrors.NotFound);
        }

        var filter = Builders<EmployeeOrder>.Filter.And(
            Builders<EmployeeOrder>.Filter.Eq(o => o.CompanyId, companyId.Value),
            Builders<EmployeeOrder>.Filter.Eq(o => o.EmployeeId, employeeId));

        return Result.Success(await GetPagedAsync(filter, pagination, cancellationToken));
    }

    private async Task<Guid?> GetEmployeeIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.Person.UserId == userId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> IsPastCutoffAsync(EmployeeOrder order, CancellationToken cancellationToken)
    {
        var businessNow = _dateTimeProvider.BusinessNow;
        var today = DateOnly.FromDateTime(businessNow.DateTime).ToString("yyyy-MM-dd");

        if (string.CompareOrdinal(order.ServiceDate, today) > 0)
        {
            return false;
        }

        if (string.CompareOrdinal(order.ServiceDate, today) < 0)
        {
            return true;
        }

        var restaurant = await _context.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == order.RestaurantId, cancellationToken);

        var cutoff = restaurant?.GetCutoff(order.MealType);
        return cutoff.HasValue && TimeOnly.FromDateTime(businessNow.DateTime) > cutoff.Value;
    }

    private async Task<PagedResult<EmployeeOrderDto>> GetPagedAsync(FilterDefinition<EmployeeOrder> filter, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var totalCount = await _orders.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var orders = await _orders.Find(filter)
            .SortByDescending(o => o.OrderedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Limit(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeOrderDto>(
            orders.Select(ToDto).ToList(),
            (int)totalCount,
            pagination.PageNumber,
            pagination.PageSize);
    }

    /// <summary>Clave única mientras el pedido está activo: un pedido por colaborador, día y comida.</summary>
    private static string BuildActiveSlot(Guid employeeId, string serviceDate, MealType mealType) =>
        $"{employeeId:N}:{serviceDate}:{mealType}";

    private static string GenerateTicketNumber(DateOnly serviceDate)
    {
        var suffix = new char[TicketSuffixLength];
        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = TicketChars[RandomNumberGenerator.GetInt32(TicketChars.Length)];
        }

        return $"ATE-{serviceDate:yyMMdd}-{new string(suffix)}";
    }

    private static EmployeeOrderDto ToDto(EmployeeOrder order)
    {
        return new EmployeeOrderDto(
            order.Id,
            order.TicketNumber,
            order.EmployeeId,
            order.EmployeeCode,
            order.EmployeeName,
            order.Department,
            order.RestaurantId,
            order.RestaurantName,
            MealTypeCodes.ToCode(order.MealType),
            order.ServiceDate,
            order.Items
                .Select(i => new EmployeeOrderItemDto(i.DishId, i.Name, i.Description, i.Protein, i.Quantity, i.UnitPrice, i.Subtotal, i.WithRice, i.Drink, i.Notes))
                .ToList(),
            order.TotalAmount,
            order.Currency,
            order.Status,
            order.StatusHistory
                .Select(h => new EmployeeOrderStatusChangeDto(h.Status, h.ChangedAt, h.Note))
                .ToList(),
            order.Notes,
            order.DeliveryMode,
            order.DeliveryAddress,
            order.OrderedAt,
            order.UpdatedAt);
    }
}
