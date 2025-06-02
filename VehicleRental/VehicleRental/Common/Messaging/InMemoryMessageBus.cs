using System.Threading.Channels;

namespace VehicleRental.Common.Messaging;

public interface IMessageBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class, IIntegrationEvent;
}

internal sealed class InMemoryMessageBus(InMemoryMessageQueue messageQueue) : IMessageBus
{
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class, IIntegrationEvent
    {
        await messageQueue.Writer.WriteAsync(message, cancellationToken);
    }
}

internal sealed class InMemoryMessageQueue
{
    private readonly Channel<IIntegrationEvent> _channel = Channel.CreateUnbounded<IIntegrationEvent>();

    public ChannelWriter<IIntegrationEvent> Writer => _channel.Writer;

    public ChannelReader<IIntegrationEvent> Reader => _channel.Reader;
}