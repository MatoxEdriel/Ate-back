namespace Ate.Domain.Constants;

/// <summary>Departamentos con los que arranca una empresa nueva; RRHH puede renombrarlos o eliminarlos.</summary>
public static class DefaultDepartments
{
    public static readonly IReadOnlyList<string> Names =
    [
        "Administración",
        "Finanzas",
        "Operaciones",
        "Talento Humano",
        "Tecnología",
        "Ventas"
    ];
}
