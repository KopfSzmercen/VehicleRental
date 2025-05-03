using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles.Infrastructure;

internal sealed class InMemoryVehiclesRepository : IVehicleRepository
{
    private readonly List<Vehicle> _vehicles = new();

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = _vehicles.FirstOrDefault(v => v.Id == id);
        return Task.FromResult(vehicle)!;
    }

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        if (_vehicles.Any(v => v.Id == vehicle.Id))
            throw new InvalidOperationException($"Vehicle with ID {vehicle.Id} already exists.");

        _vehicles.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        var existingVehicle = _vehicles.FirstOrDefault(v => v.Id == vehicle.Id);
        if (existingVehicle is null) throw new KeyNotFoundException($"Vehicle with ID {vehicle.Id} not found.");

        _vehicles.Remove(existingVehicle);
        _vehicles.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = _vehicles.FirstOrDefault(v => v.Id == id);
        if (vehicle is null) throw new KeyNotFoundException($"Vehicle with ID {id} not found.");

        _vehicles.Remove(vehicle);
        return Task.CompletedTask;
    }

    public Task<bool> IsUniqueByRegistrationNumberAsync(string registrationNumber,
        CancellationToken cancellationToken = default)
    {
        var isUnique = _vehicles.All(v => v.RegistrationNumber != registrationNumber);
        return Task.FromResult(isUnique);
    }
}