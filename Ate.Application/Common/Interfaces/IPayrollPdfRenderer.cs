using Ate.Domain.Entities;

namespace Ate.Application.Common.Interfaces;

/// <summary>
/// PDF de los cortes. Los de empresa (RRHH) nunca incluyen el detalle de pedidos: solo cédula, nombre y total.
/// </summary>
public interface IPayrollPdfRenderer
{
    byte[] RenderEmployeeSettlement(PayrollSettlement settlement, string companyName);
    byte[] RenderCompanyMonthly(string companyName, string month, IReadOnlyList<PayrollSettlement> settlements);
    byte[] RenderCompanyTermination(string companyName, PayrollSettlement settlement);
}
