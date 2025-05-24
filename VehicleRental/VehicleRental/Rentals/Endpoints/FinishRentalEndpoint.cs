using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.Endpoints;

internal sealed class FinishRentalEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("{rentalId:guid}/finish", Handle)
            .WithSummary("Finish an existing rental")
            .RequireAuthorization();
    }

    private static async Task<Results<
        NotFound<string>,
        BadRequest<string>,
        Ok<Guid>
    >> Handle(
        [FromRoute] Guid rentalId,
        [FromServices] IRentalsVehicleRepository rentalsVehicleRepository,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirst("UserId")!.Value!;

        var rentalVehicle = await rentalsVehicleRepository.GetByRentalIdAsync(rentalId, cancellationToken);

        if (rentalVehicle?.Rental is null || rentalVehicle.Rental.CustomerId != Guid.Parse(userIdString))
            return TypedResults.NotFound($"Rental with ID {rentalId} does not exist.");

        rentalVehicle.CompleteRental(timeProvider.GetUtcNow().ToUniversalTime());

        await rentalsVehicleRepository.UpdateAsync(rentalVehicle, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(rentalId);
    }
}