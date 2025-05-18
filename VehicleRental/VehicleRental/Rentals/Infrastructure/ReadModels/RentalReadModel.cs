using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.Infrastructure.ReadModels;

public sealed record RentalReadModel
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public Guid CustomerId { get; init; }

    public DateTimeOffset StartDate { get; init; }

    public DateTimeOffset EndDate { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    public RentalStatus Status { get; init; }
}