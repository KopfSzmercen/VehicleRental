namespace VehicleRental.Rentals.Domain;

internal interface IRentalsVehicleRepository
{
    Task<RentalsVehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(RentalsVehicle rentalsVehicle, CancellationToken cancellationToken = default);

    Task UpdateAsync(RentalsVehicle rentalsVehicle, CancellationToken cancellationToken = default);

    Task DeleteAsync(RentalsVehicle rentalsVehicle, CancellationToken cancellationToken = default);

    Task<RentalsVehicle?> GetByRentalIdAsync(Guid rentalId, CancellationToken cancellationToken = default);
}