using VehicleRental.Vehicles.Domain;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles.Infrastructure.ReadModels;

internal sealed record VehicleReadModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string RegistrationNumber { get; init; } = null!;

    public VehicleStatus Status { get; init; }

    public bool IsAvailableForRental { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public IReadOnlyList<VehicleLegalDocumentReadModel> LegalDocuments { get; init; } = null!;

    public IReadOnlyList<VehicleFailureReadModel> Failures { get; init; } = null!;

    public GeoLocalization? CurrentGeoLocalization { get; init; }
}