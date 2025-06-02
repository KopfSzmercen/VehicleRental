using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Reservations;

namespace VehicleRental.Rentals.Endpoints.Reservations;

public sealed class CreateReservationEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .WithSummary("Create a new reservation for a vehicle")
            .RequireAuthorization()
            .WithRequestValidation<Request>();
    }

    private static async Task<Results<
            Ok<Guid>,
            BadRequest<string>>
    > Handle(
        [FromBody] Request request,
        [FromServices] IRentalsVehicleRepository rentalsRepository,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken
    )
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value!;

        var rentalVehicle = await rentalsRepository.GetByIdAsync(request.VehicleId, cancellationToken);

        if (rentalVehicle is null) return TypedResults.BadRequest("Vehicle not found.");

        var reservation = Reservation.CreateNew(
            rentalVehicle.Id,
            request.StartDate,
            request.DurationInSeconds,
            timeProvider.GetUtcNow().ToUniversalTime(),
            Guid.Parse(userId)
        );

        rentalVehicle.Reserve(reservation, timeProvider.GetUtcNow().ToUniversalTime());

        await rentalsRepository.UpdateAsync(rentalVehicle, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(reservation.Id);
    }

    internal sealed class CreateReservationEndpointRequestValidator : AbstractValidator<Request>
    {
        public CreateReservationEndpointRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .NotEmpty().WithMessage("Vehicle ID is required.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required.")
                .Must(startDate => startDate > DateTimeOffset.UtcNow)
                .WithMessage("Start date must be in the future.");

            RuleFor(x => x.DurationInSeconds)
                .GreaterThan(0).WithMessage("Duration must be greater than 0 seconds.");
        }
    }

    internal sealed record Request
    {
        public Guid VehicleId { get; init; }
        public DateTimeOffset StartDate { get; init; }
        public int DurationInSeconds { get; init; }
    }
}