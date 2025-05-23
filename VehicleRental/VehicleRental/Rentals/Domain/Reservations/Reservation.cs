namespace VehicleRental.Rentals.Domain.Reservations;

internal sealed class Reservation
{
    private Reservation()
    {
    }

    public Guid Id { get; private init; }

    public Guid VehicleId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset StartDate { get; private init; }

    public ReservationDurationInSeconds Duration { get; private init; } = null!;

    public bool IsActive { get; private set; }

    public Guid UserId { get; private init; }

    public static Reservation CreateNew(
        Guid vehicleId,
        DateTimeOffset startDate,
        ReservationDurationInSeconds duration,
        DateTimeOffset now,
        Guid userId
    )
    {
        if (startDate < now)
            throw new ArgumentException("Start date must be in the future.");

        return new Reservation
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            CreatedAt = now,
            UpdatedAt = now,
            StartDate = startDate,
            Duration = duration,
            UserId = userId,
            IsActive = true
        };
    }

    public void Cancel(DateTimeOffset now)
    {
        if (!IsActive)
            throw new InvalidOperationException("Reservation is not active.");

        IsActive = false;
        CancelledAt = now;
        UpdatedAt = now;
    }

    public void Invalidate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}