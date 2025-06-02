using VehicleRental.Common.Messaging;

namespace VehicleRental.IntegrationEvents;

internal sealed record VehicleCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid VehicleId { get; init; }

    public DateTimeOffset VehicleCreatedAt { get; init; }
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}