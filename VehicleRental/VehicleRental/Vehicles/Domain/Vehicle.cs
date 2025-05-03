using VehicleRental.Common.ErrorHandling;

namespace VehicleRental.Vehicles.Domain;

internal sealed class Vehicle
{
    private List<VehicleLegalDocument> _legalDocuments = [];

    private Vehicle()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string RegistrationNumber { get; private set; } = null!;

    public VehicleStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public GeoLocalization? CurrentGeoLocalization { get; private set; }

    public IReadOnlyList<VehicleLegalDocument> LegalDocuments => _legalDocuments.AsReadOnly();

    public bool IsAvailableForRental { get; private set; }

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
            CreatedAt = now,
            IsAvailableForRental = false,
            _legalDocuments = []
        };
    }

    public void MakeAvailable(DateTimeOffset now)
    {
        if (Status is not VehicleStatus.Archived)
            throw new BusinessRuleValidationException("Can not modifty archived vehicle.");

        if (_legalDocuments.Count == 0)
            throw new BusinessRuleValidationException(
                "To make available vehicle must have at least one legal document.");

        Status = VehicleStatus.Available;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        IsAvailableForRental = false;
        Status = VehicleStatus.Archived;
        UpdatedAt = now;
        CurrentGeoLocalization = null;
    }

    public void PutToMaintenance(DateTimeOffset now)
    {
        if (Status is not VehicleStatus.Available)
            throw new BusinessRuleValidationException("Can not put vehicle to maintenance.");

        Status = VehicleStatus.InMaintenance;
        UpdatedAt = now;
    }

    public void AddLegalDocument(VehicleLegalDocument legalDocument)
    {
        _legalDocuments.Add(legalDocument);
    }

    public void UpdateGeoLocalization(GeoLocalization geoLocalization)
    {
        CurrentGeoLocalization = geoLocalization;
    }
}