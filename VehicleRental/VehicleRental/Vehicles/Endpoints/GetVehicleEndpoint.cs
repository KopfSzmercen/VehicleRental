using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Common.Endpoints;
using VehicleRental.Common.Pagination;
using VehicleRental.Persistence;

namespace VehicleRental.Vehicles.Endpoints;

internal sealed class GetVehicleEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("{id:guid}", Handle)
            .WithSummary("Get vehicle by id")
            .RequireAuthorization();
    }

    private static async Task<Ok<VehicleDto>> Handle(
        Guid id,
        [FromServices] AppReadDbContext dbContext
    )
    {
        var vehicle = await dbContext.Vehicles
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new VehicleDto
            {
                Id = x.Id,
                Name = x.Name,
                RegistrationNumber = x.RegistrationNumber,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CurrentGeoLocalization = x.CurrentGeoLocalization != null
                    ? new GeoLocalizationDto
                    {
                        Latitude = x.CurrentGeoLocalization.Latitude,
                        Longitude = x.CurrentGeoLocalization.Longitude
                    }
                    : null
            })
            .FirstOrDefaultAsync();

        return TypedResults.Ok(vehicle);
    }

    public sealed record GeoLocalizationDto
    {
        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }

    public sealed record VehicleDto : IEntityWithId
    {
        public string Name { get; init; } = null!;

        public string RegistrationNumber { get; init; } = null!;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public GeoLocalizationDto? CurrentGeoLocalization { get; init; }
        public Guid Id { get; init; }
    }
}