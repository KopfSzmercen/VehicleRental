using VehicleRental.Common.Pagination;

namespace VehicleRental.Vehicles.Infrastructure.ReadModels;

internal sealed class VehicleLegalDocumentReadModel : IEntityWithId
{
    public string Name { get; init; } = null!;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ValidTo { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public Guid VehicleId { get; init; }

    public VehicleReadModel Vehicle { get; init; } = null!;
    public Guid Id { get; init; }
}