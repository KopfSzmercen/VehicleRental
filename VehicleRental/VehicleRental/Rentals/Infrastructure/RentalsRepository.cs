using Microsoft.EntityFrameworkCore;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.Infrastructure;

internal sealed class RentalsRepository(AppDbContext dbContext) : IRentalsRepository
{
    public async Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Rentals
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<Rental> UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        dbContext.Rentals.Update(rental);
        return Task.FromResult(rental);
    }

    public async Task<Rental> AddAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        await dbContext.Rentals.AddAsync(rental, cancellationToken);
        return rental;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rental = await dbContext.Rentals
            .Where(r => r.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (rental is null)
            return;

        dbContext.Rentals.Remove(rental);
    }
}