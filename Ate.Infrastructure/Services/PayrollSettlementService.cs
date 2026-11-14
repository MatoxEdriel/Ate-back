using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Payroll;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using MongoDB.Driver;

namespace Ate.Infrastructure.Services;

public class PayrollSettlementService : IPayrollSettlementService, IScopedDependency
{
    private static readonly string[] ChargeableStatuses =
        [EmployeeOrderStatus.Confirmed, EmployeeOrderStatus.Ready, EmployeeOrderStatus.Delivered];

    private readonly IMongoCollection<EmployeeOrder> _orders;
    private readonly IMongoCollection<PayrollSettlement> _settlements;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PayrollSettlementService(IMongoDbContext mongoContext, IDateTimeProvider dateTimeProvider)
    {
        _orders = mongoContext.GetCollection<EmployeeOrder>(MongoCollections.EmployeeOrders);
        _settlements = mongoContext.GetCollection<PayrollSettlement>(MongoCollections.PayrollSettlements);
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> GenerateMonthlyAsync(DateOnly monthStart, CancellationToken cancellationToken = default)
    {
        monthStart = new DateOnly(monthStart.Year, monthStart.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var orders = await GetChargeableOrdersAsync(
            Builders<EmployeeOrder>.Filter.Empty, monthStart, monthEnd, cancellationToken);

        var created = 0;
        foreach (var employeeOrders in orders.GroupBy(o => o.EmployeeId))
        {
            var lastSettledEnd = await GetLastSettledEndAsync(employeeOrders.Key, monthStart, monthEnd, cancellationToken);
            var start = SettlementRules.MonthlyStart(monthStart, lastSettledEnd);
            if (start > monthEnd)
            {
                continue;
            }

            var settlement = SettlementRules.Build(
                SettlementType.Monthly, start, monthEnd, employeeOrders.ToList(), _dateTimeProvider.UtcNow, null);

            if (settlement != null && await TryInsertAsync(settlement, cancellationToken))
            {
                created++;
            }
        }

        return created;
    }

    public async Task<Result<PayrollSettlement?>> GenerateTerminationAsync(Guid employeeId, DateOnly exitDate, Guid? generatedByUserId, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(exitDate.Year, exitDate.Month, 1);
        var lastSettledEnd = await GetLastSettledEndAsync(employeeId, monthStart, exitDate, cancellationToken);
        var start = SettlementRules.TerminationStart(exitDate, lastSettledEnd);
        if (start > exitDate)
        {
            return Result.Success<PayrollSettlement?>(null);
        }

        var orders = await GetChargeableOrdersAsync(
            Builders<EmployeeOrder>.Filter.Eq(o => o.EmployeeId, employeeId), start, exitDate, cancellationToken);

        var settlement = SettlementRules.Build(
            SettlementType.Termination, start, exitDate, orders, _dateTimeProvider.UtcNow, generatedByUserId);

        if (settlement == null)
        {
            return Result.Success<PayrollSettlement?>(null);
        }

        if (!await TryInsertAsync(settlement, cancellationToken))
        {
            // Ya existía: se devuelve el corte guardado, que es el que vale.
            settlement = await _settlements
                .Find(s => s.EmployeeId == employeeId && s.Type == SettlementType.Termination && s.PeriodStart == settlement.PeriodStart)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Result.Success<PayrollSettlement?>(settlement);
    }

    private async Task<List<EmployeeOrder>> GetChargeableOrdersAsync(
        FilterDefinition<EmployeeOrder> scope, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var filter = Builders<EmployeeOrder>.Filter;
        return await _orders
            .Find(filter.And(
                scope,
                filter.In(o => o.Status, ChargeableStatuses),
                filter.Gte(o => o.ServiceDate, SettlementRules.Format(from)),
                filter.Lte(o => o.ServiceDate, SettlementRules.Format(to))))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Fin del último corte del colaborador que terminó dentro del rango, si existe.</summary>
    private async Task<DateOnly?> GetLastSettledEndAsync(Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var fromText = SettlementRules.Format(from);
        var toText = SettlementRules.Format(to);

        var filter = Builders<PayrollSettlement>.Filter;
        var last = await _settlements
            .Find(filter.And(
                filter.Eq(s => s.EmployeeId, employeeId),
                filter.Gte(s => s.PeriodEnd, fromText),
                filter.Lte(s => s.PeriodEnd, toText)))
            .SortByDescending(s => s.PeriodEnd)
            .Project(s => s.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        return last == null ? null : SettlementRules.Parse(last);
    }

    /// <summary>Inserta el corte. Devuelve false si ya existía (índice único).</summary>
    private async Task<bool> TryInsertAsync(PayrollSettlement settlement, CancellationToken cancellationToken)
    {
        try
        {
            await _settlements.InsertOneAsync(settlement, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}
