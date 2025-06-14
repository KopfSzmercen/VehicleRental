using System.Runtime.CompilerServices;
using VehicleRental.Common;
using VehicleRental.Common.Messaging;
using VehicleRental.Persistence;
using VehicleRental.Rentals;
using VehicleRental.Users;
using VehicleRental.Vehicles;

[assembly: InternalsVisibleTo("VehicleRental.Tests.Integration")]
[assembly: InternalsVisibleTo("VehicleRental.Tests.Unit")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.CreateSchemaReferenceId = info =>
    {
        var declaringTypeName = info.Type.DeclaringType?.Name;

        if (declaringTypeName is null) return null;

        return string.IsNullOrEmpty(declaringTypeName) ? info.Type.Name : $"{declaringTypeName}.{info.Type.Name}";
    };
});

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddVehiclesModule(builder.Configuration);
builder.Services.AddRentalsModule(builder.Configuration);

builder.Services.AddMessaging();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "VehicleRental"));
}

app.UseHttpsRedirection();

app.UseUsersModule();
app.UseVehiclesModule();
app.UseRentalsModule();

await app.ApplyMigrations();

app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();

namespace VehicleRental
{
    public class Program
    {
    }
}