using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Common.Pagination;
using VehicleRental.Persistence;
using VehicleRental.Users.Domain;

namespace VehicleRental.Rentals.Endpoints.Reservations;

public sealed class BrowseReservationsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithSummary("Browse reservations")
            .RequireAuthorization()
            .WithRequestValidation<Request>();
    }

    private static async Task<Results<
            Ok<IPaginatedEntity<BrowseReservationItemResponse>>,
            BadRequest<string>>
    > Handle(
        [AsParameters] Request request,
        [AsParameters] PaginationQuery paginationQuery,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AppReadDbContext dbContext
    )
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirst("UserId")!.Value!;

        var userRole = httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value!;

        if (userRole == UserRole.User)
            return await HandleNormalUserRequest(request, paginationQuery, Guid.Parse(userIdString), dbContext);

        return await HandleAdminRequest(request, paginationQuery, dbContext);
    }

    private static async Task<Ok<IPaginatedEntity<BrowseReservationItemResponse>>> HandleAdminRequest(
        Request request,
        PaginationQuery paginationQuery,
        AppReadDbContext dbContext
    )
    {
        var reservationsQueryable = dbContext.Reservations.AsQueryable();

        if (request.UsersIds is not null && request.UsersIds.Length != 0)
            reservationsQueryable = reservationsQueryable.Where(r => request.UsersIds.Contains(r.UserId));

        if (request.VehiclesIds is not null && request.VehiclesIds.Length != 0)
            reservationsQueryable = reservationsQueryable.Where(r => request.VehiclesIds.Contains(r.VehicleId));

        var reservations = await reservationsQueryable.Select(r => new BrowseReservationItemResponse
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                CurrentVehicleId = r.CurrentVehicleId,
                UserId = r.UserId,
                StartDate = r.StartDate,
                Duration = r.Duration,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                CancelledAt = r.CancelledAt
            })
            .PaginateAsync(paginationQuery.PageNumber, paginationQuery.PageSize);

        return TypedResults.Ok(reservations);
    }

    private static async Task<Ok<IPaginatedEntity<BrowseReservationItemResponse>>> HandleNormalUserRequest(
        Request request,
        PaginationQuery paginationQuery,
        Guid userId,
        AppReadDbContext dbContext
    )
    {
        var reservationsQueryable = dbContext.Reservations
            .Where(r => r.UserId == userId);

        if (request.VehiclesIds is not null && request.VehiclesIds.Length != 0)
            reservationsQueryable = reservationsQueryable.Where(r => request.VehiclesIds.Contains(r.VehicleId));

        var reservations = await reservationsQueryable.Select(r => new BrowseReservationItemResponse
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                CurrentVehicleId = r.CurrentVehicleId,
                UserId = r.UserId,
                StartDate = r.StartDate,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                CancelledAt = r.CancelledAt,
                Duration = r.Duration
            })
            .PaginateAsync(paginationQuery.PageNumber, paginationQuery.PageSize);

        return TypedResults.Ok(reservations);
    }

    public sealed record BrowseReservationItemResponse : IEntityWithId
    {
        public Guid VehicleId { get; init; }
        public Guid? CurrentVehicleId { get; init; }
        public Guid UserId { get; init; }
        public DateTimeOffset StartDate { get; init; }

        public int Duration { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public DateTimeOffset? CancelledAt { get; init; }

        public Guid Id { get; init; }
    }

    public sealed record Request
    {
        [FromQuery] public Guid[]? VehiclesIds { get; set; }
        [FromQuery] public Guid[]? UsersIds { get; set; }
    }
}