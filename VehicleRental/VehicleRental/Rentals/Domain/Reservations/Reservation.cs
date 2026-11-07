using VehicleRental.Common.ErrorHandling;

namespace VehicleRental.Rentals.Domain.Reservations;

internal sealed class Reservation
{
    private Reservation()
    {
    }

    public Guid Id { get; private init; }

    public Guid VehicleId { get; private init; }

    public Guid? CurrentVehicleId { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset StartDate { get; private init; }

    public ReservationDurationInSeconds Duration { get; private init; } = null!;

    public ReservationStatus Status { get; private set; }

    public Guid UserId { get; private init; }

    public DateTimeOffset EndDate => StartDate.AddSeconds(Duration.Value);

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
            Status = ReservationStatus.Active,
            CurrentVehicleId = vehicleId
        };
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return HasWindowEnded(StartDate, Duration, now);
    }

    // Shared with the read side, which shows a stored Active reservation past its window as Expired.
    public static bool HasWindowEnded(DateTimeOffset startDate, int durationInSeconds, DateTimeOffset now)
    {
        return now >= startDate.AddSeconds(durationInSeconds);
    }

    public bool IsWithinWindow(DateTimeOffset date)
    {
        return date >= StartDate && date < EndDate;
    }

    public void Cancel(DateTimeOffset now)
    {
        End(ReservationStatus.Cancelled, now);
        CancelledAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        End(ReservationStatus.Expired, now);
    }

    public void Fulfil(DateTimeOffset now)
    {
        End(ReservationStatus.Fulfilled, now);
    }

    private void End(ReservationStatus endStatus, DateTimeOffset now)
    {
        if (Status != ReservationStatus.Active)
            throw new BusinessRuleValidationException("Reservation is not active.");

        Status = endStatus;
        UpdatedAt = now;
        CurrentVehicleId = null;
    }
}
