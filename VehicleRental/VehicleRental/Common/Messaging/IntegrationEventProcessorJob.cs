namespace VehicleRental.Common.Messaging;

internal class IntegrationEventProcessorJob(
    InMemoryMessageQueue messageQueue,
    ILogger<IntegrationEventProcessorJob> logger,
    IServiceScopeFactory serviceScopeFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var integrationEvent in messageQueue.Reader.ReadAllAsync(stoppingToken))
            try
            {
                logger.LogInformation("Processing integration event: {EventId} at {CreatedAt}", integrationEvent.Id,
                    integrationEvent.CreatedAt);

                var eventType = integrationEvent.GetType();

                await using var scope = serviceScopeFactory.CreateAsyncScope();

                var handlerServiceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                var handlers = scope.ServiceProvider.GetServices(handlerServiceType);

                foreach (var handler in handlers)
                {
                    await using var handlerScope = serviceScopeFactory.CreateAsyncScope();

                    var handleMethod =
                        handlerServiceType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync));
                    if (handleMethod is null)
                    {
                        logger.LogError("Handler {HandlerType} does not implement HandleAsync method",
                            handlerServiceType);
                        continue;
                    }

                    var task = (Task)handleMethod.Invoke(handler, [integrationEvent, stoppingToken])!;
                    await task;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing integration event: {EventId}", integrationEvent.Id);
            }
    }
}