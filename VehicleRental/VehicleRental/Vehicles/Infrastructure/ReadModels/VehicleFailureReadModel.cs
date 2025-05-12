using VehicleRental.Vehicles.Domain.VehicleFailures;

namespace VehicleRental.Vehicles.Infrastructure.ReadModels;

internal sealed record VehicleFailureReadModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public Guid VehicleId { get; init; }

    public VehicleReadModel Vehicle { get; init; } = null!;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public VehicleFailureStatus Status { get; init; }

    public Guid CreatorId { get; private init; }
}