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
        [FromServices] IRentalsRepository rentalsRepository,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirst("UserId")!.Value!;

        var rental = await rentalsRepository.GetByIdAsync(rentalId, cancellationToken);

        if (rental is null || rental.CustomerId != Guid.Parse(userIdString))
            return TypedResults.NotFound($"Rental with ID {rentalId} does not exist.");

        rental.Complete(timeProvider.GetLocalNow());

        await rentalsRepository.UpdateAsync(rental, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(rental.Id);
    }
}