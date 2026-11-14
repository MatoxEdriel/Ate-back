using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Payroll;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ate.Infrastructure.Services;

/// <summary>
/// Cierre mensual de nómina. Revisa cada hora (y al arrancar) si toca cerrar el mes anterior (desde el día
/// <see cref="SettlementRules.CloseDay"/>). Cada mes se cierra una sola vez: queda registrado en payroll_closes.
/// </summary>
public sealed class PayrollCloseWorker : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<PayrollCloseWorker> _logger;
    private DateOnly? _lastClosedMonth;

    public PayrollCloseWorker(IServiceScopeFactory scopeFactory, IDateTimeProvider dateTimeProvider, ILogger<PayrollCloseWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            await CloseLastMonthAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CloseLastMonthAsync(CancellationToken stoppingToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.BusinessNow.DateTime);
        var month = SettlementRules.MonthToClose(today);
        if (month == null || _lastClosedMonth == month)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var payroll = scope.ServiceProvider.GetRequiredService<IPayrollSettlementService>();
            var created = await payroll.CloseMonthAsync(month.Value, stoppingToken);
            _lastClosedMonth = month;
            if (created.HasValue)
            {
                _logger.LogInformation("Payroll close for {Month}: {Count} settlements created.", month.Value.ToString("yyyy-MM"), created.Value);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payroll close for {Month} failed; it will be retried.", month.Value.ToString("yyyy-MM"));
        }
    }
}
