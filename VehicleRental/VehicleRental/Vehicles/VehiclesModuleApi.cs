using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles;

internal interface IVehiclesModuleApi
{
    public Task<bool> IsAvailableForRentalAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}

internal sealed class VehiclesModuleApi(IVehicleRepository vehiclesRepository) : IVehiclesModuleApi
{
    public async Task<bool> IsAvailableForRentalAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var status = await vehiclesRepository.GetStatusByIdAsync(vehicleId, cancellationToken);

        return status is VehicleStatus.Available;
    }
}
