using Microsoft.EntityFrameworkCore;
using VehicleRental.Persistence;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles.Infrastructure;

internal sealed class VehiclesRepository(AppDbContext dbContext) : IVehicleRepository
{
    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await dbContext.Vehicles.AddAsync(vehicle, cancellationToken);
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        dbContext.Vehicles.Update(vehicle);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await GetByIdAsync(id, cancellationToken);
        if (vehicle is null) return;

        dbContext.Vehicles.Remove(vehicle);
    }

    public async Task<bool> IsUniqueByRegistrationNumberAsync(string registrationNumber,
        CancellationToken cancellationToken = default)
    {
        return !await dbContext.Vehicles
            .AnyAsync(v => v.RegistrationNumber == registrationNumber, cancellationToken);
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Vehicles
            .AnyAsync(v => v.Id == id, cancellationToken);
    }
}