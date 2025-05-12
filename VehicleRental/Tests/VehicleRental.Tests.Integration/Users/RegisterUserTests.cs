using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Users.Endpoints;

namespace VehicleRental.Tests.Integration.Users;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class RegisterUserTests(TestWebApplication testWebApplication) : IDisposable
{
    private readonly HttpClient _client = testWebApplication.CreateClient();

    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenEmailIsUnique_RegisterUser_ShouldSucceed()
    {
        // Arrange
        var registerUserRequest = new RegisterUserEndpoint.Request
        {
            Email = "email@t.pl",
            Password = "password"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/users", registerUserRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenEmailIsNotUnique_RegisterUser_ShouldFail()
    {
        // Arrange
        var registerUserRequest = new RegisterUserEndpoint.Request
        {
            Email = "email@t.pl",
            Password = "password"
        };

        await _client.PostAsJsonAsync("/users", registerUserRequest);

        // Act
        var response = await _client.PostAsJsonAsync("/users", registerUserRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}