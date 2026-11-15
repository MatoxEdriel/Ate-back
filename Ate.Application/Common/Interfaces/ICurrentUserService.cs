namespace Ate.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>IP del cliente (la real si hay un proxy de confianza configurado).</summary>
    string? IpAddress { get; }
}
