using Microsoft.EntityFrameworkCore;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.Infrastructure;

internal sealed class CompletedRentalsRepository(AppDbContext dbContext) : ICompletedRentalsRepository
{
    public async Task<Rental?> GetByIdAsync(Guid rentalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Rental>()
            .FirstOrDefaultAsync(r => r.Id == rentalId && r.Status == RentalStatus.Completed, cancellationToken);
    }

    public async Task<bool> HasUnpaidRentalsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Rental>()
            .AnyAsync(r =>
                    r.CustomerId == customerId &&
                    r.Status == RentalStatus.Completed &&
                    r.PaymentStatus == PaymentStatus.Unpaid,
                cancellationToken);
    }

    public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        dbContext.Set<Rental>().Update(rental);
        return Task.CompletedTask;
    }
}
