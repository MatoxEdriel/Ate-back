using Ate.Application.Features.Payroll.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Entities;

namespace Ate.Application.Common.Interfaces;

public interface IPayrollSettlementService
{
    /// <summary>
    /// Genera los cortes mensuales del mes que empieza en <paramref name="monthStart"/>.
    /// Es idempotente: los cortes que ya existen se omiten. Devuelve cuántos creó.
    /// </summary>
    Task<int> GenerateMonthlyAsync(DateOnly monthStart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera el corte de liquidación de un colaborador hasta su fecha de salida.
    /// Lo usará el flujo de salida (No vigente); hoy nada lo llama.
    /// </summary>
    Task<Result<PayrollSettlement?>> GenerateTerminationAsync(Guid employeeId, DateOnly exitDate, Guid? generatedByUserId, CancellationToken cancellationToken = default);

    Task<Result<CurrentSpendingDto>> GetCurrentSpendingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmployeeSettlementDto>>> GetEmployeeSettlementsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<PdfFile>> GetEmployeeSettlementPdfAsync(Guid userId, string settlementId, CancellationToken cancellationToken = default);
}
