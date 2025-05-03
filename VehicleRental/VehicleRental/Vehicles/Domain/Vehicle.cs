using VehicleRental.Common.ErrorHandling;

namespace VehicleRental.Vehicles.Domain;

internal sealed class Vehicle
{
    private Vehicle()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string RegistrationNumber { get; private set; } = null!;

    public VehicleStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public GeoLocalization CurrentGeoLocalization { get; private set; } = null!;

    public static Vehicle CreateNew(
        string name,
        string registrationNumber,
        GeoLocalization geoLocalization,
        bool registrationNumberIsUnique,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Registration number cannot be null or empty.", nameof(registrationNumber));

        if (registrationNumberIsUnique == false)
            throw new BusinessRuleValidationException(
                $"Vehicle with registration number {registrationNumber} already exists.");

        return new Vehicle
        {
            Id = Guid.NewGuid(),
            Name = name,
            RegistrationNumber = registrationNumber,
            Status = VehicleStatus.InVerification,
            CurrentGeoLocalization = geoLocalization,
            CreatedAt = now
        };
    }

    public void UpdateGeoLocalization(GeoLocalization geoLocalization)
    {
        CurrentGeoLocalization = geoLocalization;
    }
}