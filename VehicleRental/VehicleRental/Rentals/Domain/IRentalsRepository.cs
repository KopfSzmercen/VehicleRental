namespace VehicleRental.Rentals.Domain;

internal interface IRentalsRepository
{
    Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Rental> UpdateAsync(Rental rental, CancellationToken cancellationToken = default);

    Task<Rental> AddAsync(Rental rental, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}