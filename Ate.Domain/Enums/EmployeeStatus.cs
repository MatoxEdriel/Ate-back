namespace Ate.Domain.Enums;

/// <summary>Estado del colaborador dentro de su empresa. Se guarda como texto.</summary>
public enum EmployeeStatus
{
    /// <summary>Activo: puede ingresar y pedir.</summary>
    Active,

    /// <summary>Inactivo: suspendido temporalmente por RRHH; se puede reactivar.</summary>
    Inactive,

    /// <summary>No vigente: ya no pertenece a la empresa (salida). No se reactiva desde el panel.</summary>
    NotCurrent
}
