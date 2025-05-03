namespace VehicleRental.Vehicles.Domain.Vehicles;

internal interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsUniqueByRegistrationNumberAsync(string registrationNumber,
        CancellationToken cancellationToken = default);
}