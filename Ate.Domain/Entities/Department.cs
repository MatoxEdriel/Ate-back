using Ate.Domain.Common;

namespace Ate.Domain.Entities;

/// <summary>
/// Área interna de una empresa (Tecnología, Bodega, Planta 2...). Cada empresa define y nombra
/// las suyas; el nombre es único dentro de la empresa sin distinguir mayúsculas.
/// </summary>
public class Department : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
