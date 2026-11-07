using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Common.Pagination;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Reservations;
using VehicleRental.Rentals.Endpoints.Reservations;
using VehicleRental.Users.Domain;

namespace VehicleRental.Tests.Integration.Reservations;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class BrowsingReservationsTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenUserIsNormalUser_BrowseReservations_ShouldReturn_OnlyCurrentUserReservations()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsUserAsync(testWebApplication);

        Guid currentUserReservationVehicleId;
        Guid currentUserId;
        using (var scope = testWebApplication.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();
            var userManager = scopedServices.GetRequiredService<UserManager<User>>();

            var currentUser = await dbContext.Users.FirstAsync();
            currentUserId = currentUser.Id;

            // Create vehicles for reservations
            var vehicle1 = RentalsVehicle.CreateNew(Guid.NewGuid(), DateTimeOffset.UtcNow);
            var vehicle2 = RentalsVehicle.CreateNew(Guid.NewGuid(), DateTimeOffset.UtcNow);

            await dbContext.RentalVehicles.AddRangeAsync(vehicle1, vehicle2);

            // Create another user
            var otherUser = User.CreateNormalUser("otherUser@test.com");
            await userManager.CreateAsync(otherUser, "userSecurePassword123");
            await userManager.AddToRoleAsync(otherUser, UserRole.User);
            var createdOtherUser = await userManager.FindByEmailAsync("otherUser@test.com");

            // Create reservation for current user
            var currentUserReservation = Reservation.CreateNew(
                vehicle1.Id,
                DateTimeOffset.UtcNow.AddDays(1),
                new ReservationDurationInSeconds(3600),
                DateTimeOffset.UtcNow,
                currentUserId
            );
            currentUserReservationVehicleId = vehicle1.Id;

            // Create reservation for other user
            var otherUserReservation = Reservation.CreateNew(
                vehicle2.Id,
                DateTimeOffset.UtcNow.AddDays(1),
                new ReservationDurationInSeconds(3600),
                DateTimeOffset.UtcNow,
                createdOtherUser!.Id
            );

            vehicle1.Reserve(currentUserReservation, true, DateTimeOffset.UtcNow);
            vehicle2.Reserve(otherUserReservation, true, DateTimeOffset.UtcNow);

            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/reservations?PageNumber=1&PageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<PaginatedEntity<BrowseReservationsEndpoint.BrowseReservationItemResponse>>();

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count().ShouldBe(1);
        result.Items.First().UserId.ShouldBe(currentUserId);
        result.Items.First().VehicleId.ShouldBe(currentUserReservationVehicleId);
    }

    [Fact]
    public async Task GivenUserIsAdmin_BrowseReservations_ShouldReturn_ReservationsOfAllUsers()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        Guid someUserReservationVehicleId;
        using (var scope = testWebApplication.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();
            var userManager = scopedServices.GetRequiredService<UserManager<User>>();

            // Create vehicles for reservations
            var vehicle1 = RentalsVehicle.CreateNew(Guid.NewGuid(), DateTimeOffset.UtcNow);
            var vehicle2 = RentalsVehicle.CreateNew(Guid.NewGuid(), DateTimeOffset.UtcNow);

            await dbContext.RentalVehicles.AddRangeAsync(vehicle1, vehicle2);

            // Create current user
            var user = User.CreateNormalUser("test@t.com");
            await userManager.CreateAsync(user, "userSecurePassword123");
            await userManager.AddToRoleAsync(user, UserRole.User);
            var createdUser = await userManager.FindByEmailAsync("test@t.com");
            var userId = createdUser!.Id;

            // Create reservation for current user
            var userReservation = Reservation.CreateNew(
                vehicle1.Id,
                DateTimeOffset.UtcNow.AddDays(1),
                new ReservationDurationInSeconds(3600),
                DateTimeOffset.UtcNow,
                userId
            );

            someUserReservationVehicleId = userReservation.VehicleId;
            vehicle1.Reserve(userReservation, true, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/reservations?PageNumber=1&PageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<PaginatedEntity<BrowseReservationsEndpoint.BrowseReservationItemResponse>>();

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count().ShouldBe(1);
        result.Items.First().UserId.ShouldNotBe(Guid.Empty);
        result.Items.First().VehicleId.ShouldBe(someUserReservationVehicleId);
        result.Items.First().Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public async Task GivenStoredActiveReservationWhoseWindowHasPassed_BrowseReservations_ShouldShowItAsExpired()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsUserAsync(testWebApplication);

        var userId = await testWebApplication.GetUserIdAsync("user@t.pl");
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var reservationId = await testWebApplication.SeedExpiredReservationAsync(vehicleId, userId);

        // Act
        var response = await client.GetAsync("/reservations?PageNumber=1&PageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<PaginatedEntity<BrowseReservationsEndpoint.BrowseReservationItemResponse>>();

        result.ShouldNotBeNull();
        var item = result.Items.Single();
        item.Id.ShouldBe(reservationId);
        item.Status.ShouldBe(ReservationStatus.Expired);

        // Browsing doesn't write: the stored row is still Active until the vehicle is used.
        (await testWebApplication.GetReservationAsync(reservationId)).Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public async Task GivenCancelledReservation_BrowseReservations_ShouldShowItAsCancelled()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsUserAsync(testWebApplication);

        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var createResponse = await client.PostAsJsonAsync("/reservations", new CreateReservationEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = DateTimeOffset.UtcNow.AddMinutes(3),
            DurationInSeconds = 3600
        });
        createResponse.EnsureSuccessStatusCode();
        var reservationId = await createResponse.Content.ReadFromJsonAsync<Guid>();
        (await client.PutAsync($"/reservations/{reservationId}/cancel", null)).EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/reservations?PageNumber=1&PageSize=10");

        // Assert
        var result = await response.Content
            .ReadFromJsonAsync<PaginatedEntity<BrowseReservationsEndpoint.BrowseReservationItemResponse>>();

        result.ShouldNotBeNull();
        result.Items.Single().Status.ShouldBe(ReservationStatus.Cancelled);
    }
}