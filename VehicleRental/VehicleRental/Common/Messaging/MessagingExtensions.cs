namespace VehicleRental.Common.Messaging;

internal static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        services.AddSingleton<InMemoryMessageQueue>();
        services.AddHostedService<IntegrationEventProcessorJob>();

        return services;
    }
}