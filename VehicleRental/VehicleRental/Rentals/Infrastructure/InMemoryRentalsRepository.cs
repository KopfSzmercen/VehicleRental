using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.Infrastructure;

internal sealed class InMemoryRentalsRepository : IRentalsRepository
{
    private readonly List<Rental> _rentals = [];

    public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rental = _rentals.FirstOrDefault(r => r.Id == id);
        return Task.FromResult(rental);
    }

    public Task<Rental> UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        var existingRental = _rentals.FirstOrDefault(r => r.Id == rental.Id);
        if (existingRental == null) return Task.FromResult(existingRental)!;
        _rentals.Remove(existingRental);
        _rentals.Add(rental);

        return Task.FromResult(existingRental);
    }

    public Task<Rental> AddAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        _rentals.Add(rental);
        return Task.FromResult(rental);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rental = _rentals.FirstOrDefault(r => r.Id == id);
        if (rental != null) _rentals.Remove(rental);
        return Task.FromResult(rental);
    }
}