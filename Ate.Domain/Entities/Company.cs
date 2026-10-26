using Ate.Domain.Common;

namespace Ate.Domain.Entities;

public class Company : BaseAuditableEntity
{
    public string BusinessName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
