using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Common.Pagination;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain.Reservations;
using VehicleRental.Rentals.Infrastructure.ReadModels;
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
        [FromServices] AppReadDbContext dbContext,
        [FromServices] TimeProvider timeProvider
    )
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirst("UserId")!.Value!;

        var userRole = httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value!;

        var reservationsQueryable = userRole == UserRole.User
            ? NormalUserQuery(request, Guid.Parse(userIdString), dbContext)
            : AdminQuery(request, dbContext);

        var now = timeProvider.GetUtcNow().ToUniversalTime();

        return TypedResults.Ok(await BrowseAsync(reservationsQueryable, paginationQuery, now));
    }

    private static IQueryable<ReservationReadModel> AdminQuery(Request request, AppReadDbContext dbContext)
    {
        var reservationsQueryable = dbContext.Reservations.AsQueryable();

        if (request.UsersIds is not null && request.UsersIds.Length != 0)
            reservationsQueryable = reservationsQueryable.Where(r => request.UsersIds.Contains(r.UserId));

        if (request.VehiclesIds is not null && request.VehiclesIds.Length != 0)
            reservationsQueryable = reservationsQueryable.Where(r => request.VehiclesIds.Contains(r.VehicleId));

        return reservationsQueryable;
    }

    private static IQueryable<ReservationReadModel> NormalUserQuery(
        Request request,
        Guid userId,
        AppReadDbContext dbContext
    )
    {
        var reservationsQueryable = dbContext.Reservations
            .Where(r => r.UserId == userId);

        if (request.VehiclesIds is not null && request.VehiclesIds.Length != 0)
            reservationsQueryable = reservationsQueryable.Where(r => request.VehiclesIds.Contains(r.VehicleId));

        return reservationsQueryable;
    }

    private static async Task<IPaginatedEntity<BrowseReservationItemResponse>> BrowseAsync(
        IQueryable<ReservationReadModel> reservationsQueryable,
        PaginationQuery paginationQuery,
        DateTimeOffset now
    )
    {
        var page = await reservationsQueryable.Select(r => new BrowseReservationItemResponse
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                CurrentVehicleId = r.CurrentVehicleId,
                UserId = r.UserId,
                StartDate = r.StartDate,
                Duration = r.Duration,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                CancelledAt = r.CancelledAt,
                Status = r.Status
            })
            .PaginateAsync(paginationQuery.PageNumber, paginationQuery.PageSize);

        // Expiry is lazy: a row stored as Active whose window has passed is shown as Expired.
        var items = page.Items
            .Select(item => item with { Status = EffectiveStatus(item, now) })
            .ToList();

        return new PaginatedEntity<BrowseReservationItemResponse>(
            items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    private static ReservationStatus EffectiveStatus(BrowseReservationItemResponse item, DateTimeOffset now)
    {
        return item.Status == ReservationStatus.Active
               && Reservation.HasWindowEnded(item.StartDate, item.Duration, now)
            ? ReservationStatus.Expired
            : item.Status;
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
        public ReservationStatus Status { get; init; }

        public Guid Id { get; init; }
    }

    public sealed record Request
    {
        [FromQuery] public Guid[]? VehiclesIds { get; set; }
        [FromQuery] public Guid[]? UsersIds { get; set; }
    }
}
