using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Common.Messaging;
using VehicleRental.IntegrationEvents;
using VehicleRental.Persistence;
using VehicleRental.Users.Domain;
using VehicleRental.Vehicles.Domain;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles.Endpoints;

internal class CreateVehicleEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin))
            .WithRequestValidation<RequestValidator>()
            .WithSummary("Creates a new vehicle (Admin only)");
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>>> Handle(
        [FromServices] IVehicleRepository vehicleRepository,
        [FromServices] IUnitOfWork unitOfWork,
        [FromBody] Request request,
        [FromServices] IMessageBus messageBus,
        [FromServices] TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var isUnique = await vehicleRepository.IsUniqueByRegistrationNumberAsync(
            request.RegistrationNumber,
            cancellationToken);

        var vehicle = Vehicle.CreateNew(
            request.Name,
            request.RegistrationNumber,
            GeoLocalization.Create(
                request.GeoLocalization.Latitude,
                request.GeoLocalization.Longitude
            ),
            isUnique,
            DateTimeOffset.UtcNow
        );

        await vehicleRepository.AddAsync(vehicle, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await messageBus.PublishAsync(
            new VehicleCreatedIntegrationEvent
            {
                VehicleId = vehicle.Id,
                VehicleCreatedAt = vehicle.CreatedAt,
                Id = Guid.NewGuid(),
                CreatedAt = timeProvider.GetUtcNow().ToUniversalTime()
            },
            cancellationToken
        );

        return TypedResults.Ok(vehicle.Id);
    }

    internal sealed record Request
    {
        public string Name { get; init; } = string.Empty;

        public string RegistrationNumber { get; init; } = string.Empty;

        public GeoLocalizationRequest GeoLocalization { get; init; } = null!;
    }

    internal sealed record GeoLocalizationRequest
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    internal sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name cannot be empty.");

            RuleFor(x => x.RegistrationNumber)
                .NotEmpty()
                .WithMessage("Registration number cannot be empty.");

            RuleFor(x => x.GeoLocalization.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90 degrees.");

            RuleFor(x => x.GeoLocalization.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180 degrees.");
        }
    }
}