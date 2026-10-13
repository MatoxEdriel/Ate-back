using Ate.Domain.Common;
using Ate.Domain.Enums;

namespace Ate.Domain.Entities;

public class Person : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; } = DocumentType.NationalId;
    public string DocumentNumber { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
