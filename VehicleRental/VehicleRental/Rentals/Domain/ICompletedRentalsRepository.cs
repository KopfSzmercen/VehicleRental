namespace VehicleRental.Rentals.Domain;

// Completed rentals as their own aggregate root. Completing a rental detaches it from its RentalsVehicle,
// so IRentalsVehicleRepository can no longer find it.
internal interface ICompletedRentalsRepository
{
    // Returns the rental only when its status is Completed.
    Task<Rental?> GetByIdAsync(Guid rentalId, CancellationToken cancellationToken = default);

    // Customer debt ("zadłużenie klienta"): whether the customer has any Completed + Unpaid rental.
    Task<bool> HasUnpaidRentalsAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default);
}
