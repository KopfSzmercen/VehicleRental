using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Vehicles;

namespace VehicleRental.Rentals.Endpoints;

internal sealed class CreateRentalEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .WithSummary("Create a new rental")
            .RequireAuthorization()
            .WithRequestValidation<Request>();
    }

    private static async Task<Results<
        Ok<Guid>,
        BadRequest<string>,
        Created
    >> Handle(
        [FromBody] Request request,
        [FromServices] IRentalsVehicleRepository rentalsRepository,
        [FromServices] IVehiclesModuleApi vehiclesModuleApi,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value!;

        // 4.2 Wczytaj pojazd
        var rentalVehicle = await rentalsRepository.GetByIdAsync(request.VehicleId, cancellationToken);

        if (rentalVehicle is null) return TypedResults.BadRequest("Vehicle not found.");

        var vehicleIsAvailable =
            await vehiclesModuleApi.IsAvailableForRentalAsync(rentalVehicle.Id, cancellationToken);

        // 4.3 Utwórz wynajem
        var rental = Rental.CreateNew(
            rentalVehicle.Id,
            Guid.Parse(userId),
            request.StartDate,
            request.EndDate,
            new Money(100, Currency.USD),
            timeProvider.GetUtcNow().ToUniversalTime()
        );

        // 4.4 Wynajmij pojazd
        rentalVehicle.Rent(rental, vehicleIsAvailable, timeProvider.GetUtcNow().ToUniversalTime());

        // 4.5 Zapisz wynajem
        await rentalsRepository.UpdateAsync(rentalVehicle, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(rental.Id);
    }

    internal sealed record Request
    {
        public Guid VehicleId { get; init; }
        public DateTimeOffset StartDate { get; init; }
        public DateTimeOffset EndDate { get; init; }
    }

    internal sealed class CreateRentalRequestValidator : AbstractValidator<Request>
    {
        public CreateRentalRequestValidator()
        {
            RuleFor(x => x.VehicleId)
                .NotEmpty()
                .WithMessage("Vehicle ID is required.");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required.")
                .Must(date => date > DateTimeOffset.UtcNow)
                .WithMessage("Start date must be in the future.");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required.")
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date.");
        }
    }
}