using Ate.Domain.Common;

namespace Ate.Domain.Entities;

public class Employee : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public string? EmployeeCode { get; set; }
    public string? Department { get; set; }
    public decimal MonthlyAllowance { get; set; }
    public string QrCodeToken { get; set; } = Guid.CreateVersion7().ToString("N");
    public bool IsActive { get; set; } = true;
}
