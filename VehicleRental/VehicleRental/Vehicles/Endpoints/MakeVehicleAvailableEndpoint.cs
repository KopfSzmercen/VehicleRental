using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Persistence;
using VehicleRental.Users.Domain;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles.Endpoints;

internal sealed class MakeVehicleAvailableEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("{vehicleId:guid}/make-available", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin))
            .WithSummary("Makes a vehicle available for rental");
    }

    private static async Task<Results<Ok, NotFound<string>, BadRequest<string>>> Handle(
        [FromRoute] Guid vehicleId,
        [FromServices] IVehicleRepository vehicleRepository,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);

        if (vehicle is null) return TypedResults.NotFound<string>("Vehicle not found.");

        vehicle.MakeAvailable(timeProvider.GetUtcNow());

        await vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}