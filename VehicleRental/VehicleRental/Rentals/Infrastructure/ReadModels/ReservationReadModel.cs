using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleRental.Rentals.Infrastructure.ReadModels;

public sealed record ReservationReadModel
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public Guid? CurrentVehicleId { get; init; }

    public Guid UserId { get; init; }

    public DateTimeOffset StartDate { get; init; }


    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    public int Duration { get; init; }

    public ReservationStatus Status { get; init; }
}