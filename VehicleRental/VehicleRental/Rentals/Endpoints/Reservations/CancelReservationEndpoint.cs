using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.Endpoints.Reservations;

internal sealed class CancelReservationEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("{reservationId:guid}/cancel", Handle)
            .WithSummary("Cancel own active reservation")
            .RequireAuthorization();
    }

    private static async Task<Results<
        NotFound<string>,
        Ok<Guid>
    >> Handle(
        [FromRoute] Guid reservationId,
        [FromServices] IRentalsVehicleRepository rentalsVehicleRepository,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(httpContextAccessor.HttpContext?.User.FindFirst("UserId")!.Value!);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var notFound = TypedResults.NotFound($"Reservation with ID {reservationId} does not exist.");

        var rentalVehicle = await rentalsVehicleRepository.GetByReservationIdAsync(reservationId, cancellationToken);

        if (rentalVehicle is not null && rentalVehicle.ExpireReservationIfDue(now))
        {
            await rentalsVehicleRepository.UpdateAsync(rentalVehicle, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return notFound;
        }

        if (rentalVehicle?.Reservation is null || rentalVehicle.Reservation.UserId != userId)
            return notFound;

        rentalVehicle.CancelReservation(reservationId, userId, now);

        await rentalsVehicleRepository.UpdateAsync(rentalVehicle, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(reservationId);
    }
}
