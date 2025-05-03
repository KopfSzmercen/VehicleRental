using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Persistence;
using VehicleRental.Users.Domain;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Vehicles.Endpoints;

internal class AddVehicleLegalDocumentEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("{vehicleId:guid}/legal-documents", Handle)
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin))
            .WithRequestValidation<RequestValidator>()
            .WithSummary("Adds a legal document to a vehicle (Admin only)");
    }

    private static async Task<Results<Ok<Guid>, NotFound<string>, BadRequest<string>>> Handle(
        [FromRoute] Guid vehicleId,
        [FromServices] IVehicleRepository vehicleRepository,
        [FromServices] IUnitOfWork unitOfWork,
        [FromBody] Request request,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);

        if (vehicle is null)
            return TypedResults.NotFound("Vehicle not found.");

        var legalDocument = VehicleLegalDocument.CreateNew(
            request.Name,
            vehicleId,
            request.ValidTo,
            DateTimeOffset.UtcNow
        );

        vehicle.AddLegalDocument(legalDocument);

        await vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(legalDocument.Id);
    }

    internal sealed record Request
    {
        public string Name { get; init; } = string.Empty;
        public DateTimeOffset ValidTo { get; init; }
    }

    internal sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Document name cannot be empty.");

            RuleFor(x => x.ValidTo)
                .GreaterThan(DateTimeOffset.UtcNow)
                .WithMessage("ValidTo date must be in the future.");
        }
    }
}