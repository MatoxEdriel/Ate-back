namespace Ate.Application.Common.Interfaces;

public interface IDishCatalogSyncService
{
    /// <summary>Reescribe en MongoDB (dish_catalog) todos los platos de Postgres. Devuelve cuántos sincronizó.</summary>
    Task<int> SyncAllAsync(CancellationToken cancellationToken = default);
}
