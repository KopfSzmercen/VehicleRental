namespace VehicleRental.Common.Messaging;

public interface IIntegrationEvent
{
    public Guid Id { get; }

    public DateTimeOffset CreatedAt { get; }
}

public interface IIntegrationEventHandler<in TIntegrationEvent> where TIntegrationEvent : IIntegrationEvent
{
    Task HandleAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}