using FluentValidation;
using VehicleRental.Users.Endpoints;
using VehicleRental.Users.Infrastructure.Auth;
using VehicleRental.Users.Infrastructure.Tokens;

namespace VehicleRental.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TokensOptions>()
            .BindConfiguration(TokensOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddValidatorsFromAssembly(typeof(UsersModule).Assembly, includeInternalTypes: true);

        services.AddAuth();

        return services;
    }

    public static WebApplication UseUsersModule(this WebApplication app)
    {
        app.AddUsersEndpoint();
        return app;
    }
}