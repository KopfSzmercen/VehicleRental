using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleRental.Rentals.Domain;

internal sealed class RentalsVehicle
{
    private RentalsVehicle()
    {
    }

    public Guid Id { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Reservation? Reservation { get; private set; }

    public Rental? Rental { get; private set; }

    public static RentalsVehicle CreateNew(Guid id, DateTimeOffset now)
    {
        return new RentalsVehicle
        {
            Id = id,
            UpdatedAt = now
        };
    }

    public void Reserve(Reservation reservation, DateTimeOffset now)
    {
        if (Reservation is not null)
            throw new BusinessRuleValidationException("Vehicle is already reserved.");

        if (Rental is not null)
            throw new BusinessRuleValidationException("Vehicle is already rented.");

        Reservation = reservation;
        UpdatedAt = now;
    }

    public void CancelReservation(DateTimeOffset now)
    {
        if (Reservation is null)
            throw new BusinessRuleValidationException("Vehicle is not reserved.");

        Reservation.Cancel(now);
        UpdatedAt = now;
        Reservation = null;
    }

    public void Rent(Rental rental, DateTimeOffset now)
    {
        if (Rental is not null)
            throw new BusinessRuleValidationException("Vehicle is already rented.");

        if (Reservation is not null)
            throw new BusinessRuleValidationException("Vehicle is reserved.");

        Rental = rental;
        UpdatedAt = now;
    }

    public void CompleteRental(DateTimeOffset now)
    {
        if (Rental is null)
            throw new BusinessRuleValidationException("Vehicle is not rented.");

        Rental.Complete(now);
        UpdatedAt = now;
        Rental = null;
    }
}