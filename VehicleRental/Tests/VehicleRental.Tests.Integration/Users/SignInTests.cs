using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Users.Endpoints;

namespace VehicleRental.Tests.Integration.Users;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class SignInTests(TestWebApplication testWebApplication) : IDisposable, IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await testWebApplication.DisposeAsync();
    }

    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }


    [Fact]
    public async Task GivenUserIsRegistered_SignIn_ShouldSucceed()
    {
        // Arrange
        var client = testWebApplication.CreateClient();
        var registerUserRequest = new RegisterUserEndpoint.Request
        {
            Email = "test@t.pl",
            Password = "password"
        };

        await client.PostAsJsonAsync("/users", registerUserRequest);

        var signInRequest = new SignInUserndpoint.Reuqest
        {
            Email = "test@t.pl",
            Password = "password"
        };

        // Act
        var response = await client.PostAsJsonAsync("users/sign-in", signInRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SignInUserndpoint.Response>();

        result.ShouldNotBeNull();
        result.Token.ShouldNotBeNull();
    }
}