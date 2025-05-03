namespace VehicleRental.Vehicles.Domain;

internal interface IVehicleRepository
{
    Task<Vehicle> GetByIdAsync(Guid id);

    Task AddAsync(Vehicle vehicle);

    Task UpdateAsync(Vehicle vehicle);

    Task DeleteAsync(Guid id);
}