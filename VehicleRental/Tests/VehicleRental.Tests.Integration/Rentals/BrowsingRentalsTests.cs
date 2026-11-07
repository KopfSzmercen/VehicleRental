using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Common.Pagination;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Endpoints;
using VehicleRental.Users.Domain;
using VehicleRental.Vehicles.Domain;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Tests.Integration.Rentals;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class BrowsingRentalsTests(TestWebApplication testWebApplication) : IDisposable
{
    void IDisposable.Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenUserIsNormalUser_BrowseRentals_ShouldReturn_OnlyCurrentUserRentals()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsUserAsync(testWebApplication);

        Guid rentalId;
        Guid userId;
        using (var scope = testWebApplication.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();
            var userManager = scopedServices.GetRequiredService<UserManager<User>>();

            var user = await dbContext.Users.FirstAsync();
            userId = user.Id;

            var vehicle = Vehicle.CreateNew(
                "Test Car",
                "TEST1234",
                GeoLocalization.Create(40.7128, -74.0060),
                true,
                DateTimeOffset.UtcNow
            );

            var vehicle2 = Vehicle.CreateNew(
                "Test Car 2",
                "TEST5678",
                GeoLocalization.Create(40.7128, -74.0060),
                true,
                DateTimeOffset.UtcNow
            );

            await dbContext.Vehicles.AddRangeAsync(vehicle, vehicle2);

            var startDate = DateTimeOffset.UtcNow.AddDays(1);
            var rental = Rental.CreateNew(
                vehicle.Id,
                user.Id,
                startDate,
                startDate.AddHours(2),
                new Money(1000, Currency.USD),
                DateTimeOffset.Now
            );
            rentalId = rental.Id;

            var otherUser = User.CreateNormalUser("otherUser@t.pl");

            await userManager.CreateAsync(otherUser, "userSecurePassword123");

            await userManager.AddToRoleAsync(otherUser, UserRole.User);

            var createdOtherUser = await userManager.FindByEmailAsync("otherUser@t.pl");

            var rentalOfOtherUser = Rental.CreateNew(
                vehicle2.Id,
                createdOtherUser!.Id,
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2),
                new Money(1000, Currency.USD),
                DateTimeOffset.Now
            );

            var rentalVehicle = RentalsVehicle.CreateNew(
                vehicle.Id,
                DateTimeOffset.Now
            );

            rentalVehicle.Rent(rental, true, DateTimeOffset.Now);

            var rentalVehicleOfOtherUser = RentalsVehicle.CreateNew(
                vehicle2.Id,
                DateTimeOffset.Now
            );

            rentalVehicleOfOtherUser.Rent(rentalOfOtherUser, true, DateTimeOffset.Now);

            await dbContext.RentalVehicles.AddRangeAsync(rentalVehicle, rentalVehicleOfOtherUser);

            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/rentals?PageNumber=1&PageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rentals = await response.Content
            .ReadFromJsonAsync<PaginatedEntity<BrowseRentalsEndpoint.BrowseRentalItemResponse>>();

        rentals.ShouldNotBeNull();
        rentals.Items.Count().ShouldBe(1);
        rentals.Items.First().UserId.ShouldBe(userId);
        rentals.Items.First().Id.ShouldBe(rentalId);
        rentals.Items.First().PriceAmount.ShouldBe(20);
        rentals.Items.First().PriceCurrency.ShouldBe("USD");
    }

    [Fact]
    public async Task GivenUserIsAdmin_BrowseRentals_ShouldReturn_AllRentals()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        using (var scope = testWebApplication.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();
            var userManager = scopedServices.GetRequiredService<UserManager<User>>();

            var otherUser = User.CreateNormalUser("normalUser@t.pl");
            await userManager.CreateAsync(otherUser, "userSecurePassword123");
            await userManager.AddToRoleAsync(otherUser, UserRole.User);
            var createdOtherUser = await userManager.FindByEmailAsync("normalUser@t.pl");

            var vehicle = Vehicle.CreateNew(
                "Test Car",
                "TEST1234",
                GeoLocalization.Create(40.7128, -74.0060),
                true,
                DateTimeOffset.UtcNow
            );

            var vehicle2 = Vehicle.CreateNew(
                "Test Car 2",
                "TEST5678",
                GeoLocalization.Create(40.7128, -74.0060),
                true,
                DateTimeOffset.UtcNow
            );

            await dbContext.Vehicles.AddRangeAsync(vehicle, vehicle2);

            var vehicleRental = RentalsVehicle.CreateNew(
                vehicle.Id,
                DateTimeOffset.Now
            );

            var startDate = DateTimeOffset.UtcNow.AddDays(1);
            var rental = Rental.CreateNew(
                vehicle.Id,
                createdOtherUser!.Id,
                startDate,
                startDate.AddHours(3),
                new Money(1000, Currency.USD),
                DateTimeOffset.Now
            );

            vehicleRental.Rent(rental, true, DateTimeOffset.Now);

            dbContext.RentalVehicles.Add(vehicleRental);

            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/rentals?PageNumber=1&PageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rentals = await response.Content
            .ReadFromJsonAsync<PaginatedEntity<BrowseRentalsEndpoint.BrowseRentalItemResponse>>();

        rentals.ShouldNotBeNull();
        rentals.Items.Count().ShouldBe(1);
        rentals.Items.First().PriceAmount.ShouldBe(30);
        rentals.Items.First().PriceCurrency.ShouldBe("USD");
    }
}