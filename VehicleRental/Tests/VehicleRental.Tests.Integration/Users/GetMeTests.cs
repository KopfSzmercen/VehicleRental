using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Users.Endpoints;

namespace VehicleRental.Tests.Integration.Users;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class GetMeTests(TestWebApplication testWebApplication) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await testWebApplication.ResetDatabaseAsync();
    }


    [Fact]
    public async Task GivenUserIsSignedIn_GetMe_ShouldSucceed()
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

        var signInResponse = await client.PostAsJsonAsync("users/sign-in", signInRequest);
        var signInResult = await signInResponse.Content.ReadFromJsonAsync<SignInUserndpoint.Response>();

        // Act
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signInResult!.Token);
        var response = await client.GetAsync("users/me");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}