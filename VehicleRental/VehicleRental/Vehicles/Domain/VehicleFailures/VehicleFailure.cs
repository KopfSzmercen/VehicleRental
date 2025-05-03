using VehicleRental.Common.ErrorHandling;

namespace VehicleRental.Vehicles.Domain.VehicleFailures;

internal sealed class VehicleFailure
{
    private VehicleFailure()
    {
    }

    public Guid Id { get; private init; }

    public string Name { get; private init; } = null!;

    public Guid VehicleId { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public VehicleFailureStatus Status { get; private set; }

    public Guid CreatorId { get; private init; }

    public static VehicleFailure CreateNew(
        string name,
        Guid vehicleId,
        Guid creatorId,
        DateTimeOffset now
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        return new VehicleFailure
        {
            Id = Guid.NewGuid(),
            Name = name,
            VehicleId = vehicleId,
            CreatorId = creatorId,
            CreatedAt = now,
            Status = VehicleFailureStatus.Open
        };
    }

    public void Resolve(DateTimeOffset now)
    {
        if (Status is not VehicleFailureStatus.Open)
            throw new BusinessRuleValidationException("Resolving possible only for open vehicle failure.");

        UpdatedAt = now;
        Status = VehicleFailureStatus.Resolved;
    }

    public void CanNotBeResolved(DateTimeOffset now)
    {
        if (Status is not VehicleFailureStatus.Open)
            throw new BusinessRuleValidationException(
                "Marking as CanNotBeResolved possible only for open vehicle failure.");

        UpdatedAt = now;
        Status = VehicleFailureStatus.CanNotBeResolved;
    }
}