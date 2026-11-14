using Ate.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ate.Infrastructure.Services;

/// <summary>
/// Cierre mensual de nómina. Revisa cada hora (y al arrancar) si el mes anterior ya se cerró en este proceso;
/// si no, genera sus cortes. La generación es idempotente, así que reiniciar el servidor no duplica cortes.
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
        var previousMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        if (_lastClosedMonth == previousMonth)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var payroll = scope.ServiceProvider.GetRequiredService<IPayrollSettlementService>();
            var created = await payroll.GenerateMonthlyAsync(previousMonth, stoppingToken);
            _lastClosedMonth = previousMonth;
            _logger.LogInformation("Payroll close for {Month}: {Count} settlements created.", previousMonth.ToString("yyyy-MM"), created);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payroll close for {Month} failed; it will be retried.", previousMonth.ToString("yyyy-MM"));
        }
    }
}
