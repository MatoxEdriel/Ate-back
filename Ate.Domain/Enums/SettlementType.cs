namespace Ate.Domain.Enums;

/// <summary>Tipo de corte de nómina. Se guarda como texto.</summary>
public enum SettlementType
{
    /// <summary>Mensual: se descuenta del sueldo.</summary>
    Monthly,

    /// <summary>Liquidación: se descuenta de la liquidación cuando el colaborador sale de la empresa.</summary>
    Termination
}
