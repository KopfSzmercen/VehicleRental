using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Common.Endpoints;
using VehicleRental.Common.Pagination;
using VehicleRental.Persistence;

namespace VehicleRental.Vehicles.Endpoints;

internal sealed class BrowseVehiclesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithSummary("Browse all vehicles")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        [AsParameters] PaginationQuery pagination,
        [FromServices] AppReadDbContext dbContext
    )
    {
        var vehicles = await dbContext.Vehicles
            .AsNoTracking()
            .Select(x => new BrowseVehiclesItemDto
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
            .PaginateAsync(
                pagination.PageNumber,
                pagination.PageSize
            );

        return Results.Ok(vehicles);
    }

    public sealed record GeoLocalizationDto
    {
        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }

    public sealed record BrowseVehiclesItemDto : IEntityWithId
    {
        public string Name { get; init; } = null!;

        public string RegistrationNumber { get; init; } = null!;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public GeoLocalizationDto? CurrentGeoLocalization { get; init; }
        public Guid Id { get; init; }
    }
}