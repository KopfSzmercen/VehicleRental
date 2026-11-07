using VehicleRental.Common.ErrorHandling;
using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleRental.Rentals.Domain;

internal sealed class RentalsVehicle
{
    private const string VehicleIsNotAvailableMessage = "Vehicle is not available for rental.";

    private RentalsVehicle()
    {
    }

    public Guid Id { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Reservation? Reservation { get; private set; }

    public Rental? Rental { get; private set; }

    public uint Version { get; set; }

    public static RentalsVehicle CreateNew(Guid id, DateTimeOffset now)
    {
        return new RentalsVehicle
        {
            Id = id,
            UpdatedAt = now
        };
    }

    public void Reserve(Reservation reservation, bool vehicleIsAvailable, DateTimeOffset now)
    {
        if (vehicleIsAvailable == false)
            throw new BusinessRuleValidationException(VehicleIsNotAvailableMessage);

        ExpireReservationIfDue(now);

        if (Reservation is not null)
            throw new BusinessRuleValidationException("Vehicle is already reserved.");

        if (Rental is not null)
            throw new BusinessRuleValidationException("Vehicle is already rented.");

        Reservation = reservation;
        UpdatedAt = now;
    }

    public bool ExpireReservationIfDue(DateTimeOffset now)
    {
        if (Reservation is null || !Reservation.IsExpired(now))
            return false;

        Reservation.Expire(now);
        Reservation = null;
        UpdatedAt = now;
        return true;
    }

    public void CancelReservation(Guid reservationId, Guid userId, DateTimeOffset now)
    {
        if (Reservation is null
            || Reservation.Id != reservationId
            || Reservation.UserId != userId
            || Reservation.Status != ReservationStatus.Active
            || Reservation.IsExpired(now))
            throw new BusinessRuleValidationException($"Reservation with ID {reservationId} does not exist.");

        Reservation.Cancel(now);
        UpdatedAt = now;
        Reservation = null;
    }

    public void Rent(Rental rental, bool vehicleIsAvailable, DateTimeOffset now)
    {
        if (vehicleIsAvailable == false)
            throw new BusinessRuleValidationException(VehicleIsNotAvailableMessage);

        if (Rental is not null)
            throw new BusinessRuleValidationException("Vehicle is already rented.");

        ExpireReservationIfDue(now);

        if (Reservation is not null)
        {
            if (Reservation.UserId != rental.CustomerId)
                throw new BusinessRuleValidationException("Vehicle is reserved.");

            if (!Reservation.IsWithinWindow(rental.StartDate))
                throw new BusinessRuleValidationException("Rental must start within the reservation window.");

            Reservation.Fulfil(now);
            Reservation = null;
        }

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