using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;

namespace VehicleRental.Rentals.Endpoints;

internal sealed class SettleRentalPaymentEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("{rentalId:guid}/settle", Handle)
            .WithSummary("Settle the outstanding payment of a completed rental")
            .RequireAuthorization();
    }

    private static async Task<Results<
        NotFound<string>,
        BadRequest<string>,
        Ok<Guid>
    >> Handle(
        [FromRoute] Guid rentalId,
        [FromServices] ICompletedRentalsRepository completedRentalsRepository,
        [FromServices] IPaymentGateway paymentGateway,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirst("UserId")!.Value!;

        // Only completed rentals are found, so an active one is "does not exist".
        var rental = await completedRentalsRepository.GetByIdAsync(rentalId, cancellationToken);

        if (rental is null || rental.CustomerId != Guid.Parse(userIdString))
            return TypedResults.NotFound($"Rental with ID {rentalId} does not exist.");

        // Checked before the charge, so a paid rental (including one the migration marked paid, which the
        // gateway never saw) is never charged.
        if (!rental.HasOutstandingPayment)
            return TypedResults.BadRequest("Rental has no outstanding payment.");

        // The only external call, made before the local transaction. The rental id is the idempotency key,
        // the same one as at finish; a declined charge doesn't use it up, so settling can retry.
        var chargeResult = await paymentGateway.ChargeAsync(
            rental.CustomerId,
            rental.Price,
            idempotencyKey: rental.Id,
            cancellationToken);

        // A declined charge throws "Payment declined." and nothing is saved.
        rental.SettlePayment(chargeResult, timeProvider.GetUtcNow().ToUniversalTime());

        await completedRentalsRepository.UpdateAsync(rental, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(rentalId);
    }
}
