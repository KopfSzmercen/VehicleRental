namespace VehicleRental.Rentals.Domain.Reservations;

public record ReservationDurationInSeconds
{
    public ReservationDurationInSeconds(int value)
    {
        Value = value switch
        {
            < 0 => throw new ArgumentOutOfRangeException(nameof(value),
                "Reservation duration must be a positive number."),
            < 60 => throw new ArgumentOutOfRangeException(nameof(value),
                "Reservation duration must be at least 60 seconds."),
            > 86400 => throw new ArgumentOutOfRangeException(nameof(value),
                "Reservation duration must be at most 86400 seconds."),
            _ => value
        };
    }

    private int Value { get; }

    public static implicit operator int(ReservationDurationInSeconds reservationDurationInSeconds)
    {
        return reservationDurationInSeconds.Value;
    }

    public static implicit operator ReservationDurationInSeconds(int value)
    {
        return new ReservationDurationInSeconds(value);
    }
}