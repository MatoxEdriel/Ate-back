namespace Ate.Domain.Entities;

/// <summary>
/// Registro de un mes ya cerrado (colección payroll_closes). Una vez existe, el cierre de ese mes no vuelve
/// a ejecutarse, aunque el servidor se reinicie: así lo que RRHH ya descargó no cambia.
/// </summary>
public class PayrollClose
{
    /// <summary>Mes cerrado, yyyy-MM.</summary>
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset ClosedAt { get; set; }
    public int SettlementsCreated { get; set; }
}
