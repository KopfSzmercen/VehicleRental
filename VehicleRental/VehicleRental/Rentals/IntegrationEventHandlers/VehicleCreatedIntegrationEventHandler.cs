using VehicleRental.Common.Messaging;
using VehicleRental.IntegrationEvents;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.IntegrationEventHandlers;

internal sealed class VehicleCreatedIntegrationEventHandler(
    IRentalsVehicleRepository rentalsRepository,
    IUnitOfWork unitOfWork
) : IIntegrationEventHandler<VehicleCreatedIntegrationEvent>
{
    public async Task HandleAsync(VehicleCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var newVehicle = RentalsVehicle.CreateNew(
            integrationEvent.VehicleId,
            integrationEvent.VehicleCreatedAt
        );

        await rentalsRepository.AddAsync(newVehicle, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}