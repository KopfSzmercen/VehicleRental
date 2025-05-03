using Microsoft.OpenApi.Models;

namespace VehicleRental.Common;

internal static class SwaggerExtensions
{
    public static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(swagger =>
        {
            swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            });

            swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description =
                    "Input your JWT Authorization header to access this API. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            swagger.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
            swagger.SupportNonNullableReferenceTypes();
        });
    }
}