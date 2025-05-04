using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles;

internal interface IVehiclesModuleApi
{
    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

internal sealed class VehiclesModuleApi(IVehicleRepository vehiclesRepository) : IVehiclesModuleApi
{
    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await vehiclesRepository.ExistsByIdAsync(id, cancellationToken);
    }
}