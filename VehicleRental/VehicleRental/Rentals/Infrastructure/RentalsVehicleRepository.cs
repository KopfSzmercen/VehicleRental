using Microsoft.EntityFrameworkCore;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.Infrastructure;

internal sealed class RentalsVehicleRepository(AppDbContext dbContext) : IRentalsVehicleRepository
{
    public async Task<RentalsVehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.RentalVehicles
            .Include(x => x.Rental)
            .Include(x => x.Reservation)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(RentalsVehicle rentalsVehicle, CancellationToken cancellationToken = default)
    {
        await dbContext.RentalVehicles.AddAsync(rentalsVehicle, cancellationToken);
    }

    public Task UpdateAsync(RentalsVehicle rentalsVehicle, CancellationToken cancellationToken = default)
    {
        dbContext.RentalVehicles.Update(rentalsVehicle);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RentalsVehicle rentalsVehicle, CancellationToken cancellationToken = default)
    {
        dbContext.RentalVehicles.Remove(rentalsVehicle);
        return Task.CompletedTask;
    }

    public async Task<RentalsVehicle?> GetByRentalIdAsync(Guid rentalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RentalVehicles
            .Include(x => x.Rental)
            .Include(x => x.Reservation)
            .FirstOrDefaultAsync(v => v.Rental != null && v.Rental.Id == rentalId, cancellationToken);
    }
}