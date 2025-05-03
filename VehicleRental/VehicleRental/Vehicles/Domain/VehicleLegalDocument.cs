using VehicleRental.Common.ErrorHandling;

namespace VehicleRental.Vehicles.Domain;

public sealed class VehicleLegalDocument
{
    private VehicleLegalDocument()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public Guid VehicleId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ValidTo { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static VehicleLegalDocument CreateNew(
        string name,
        Guid vehicleId,
        DateTimeOffset validTo,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        if (validTo < now)
            throw new BusinessRuleValidationException("ValidTo date must be in the future.");

        return new VehicleLegalDocument
        {
            Id = Guid.NewGuid(),
            Name = name,
            VehicleId = vehicleId,
            CreatedAt = now,
            ValidTo = validTo
        };
    }
}