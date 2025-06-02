using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using VehicleRental.Common.Messaging;

namespace VehicleTests.Tests.Unit.Common;

public class InMemoryMessageBusTests
{
    [Fact]
    public async Task PublishAsync_ShouldPublishMessage_AndInvokeRegisteredHandlers()
    {
        // Arrange
        var handler1 = Substitute.For<IIntegrationEventHandler<TestEvent>>();
        var handler2 = Substitute.For<IIntegrationEventHandler<TestEvent>>();

        var appBuilder = WebApplication.CreateBuilder();
        appBuilder.Services.AddSingleton(handler1);
        appBuilder.Services.AddSingleton(handler2);
        appBuilder.Services.AddMessaging();

        var app = appBuilder.Build();
        await app.StartAsync();

        var messageBus = app.Services.GetRequiredService<IMessageBus>();
        var testEvent = new TestEvent { Message = "Test message" };

        // Act
        await messageBus.PublishAsync(testEvent, CancellationToken.None);

        var timeout = TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            if (handler1.ReceivedCalls().Any() && handler2.ReceivedCalls().Any())
                break;

            await Task.Delay(50);
        }

        // Assert
        await handler1.Received(1).HandleAsync(testEvent, Arg.Any<CancellationToken>());
        await handler2.Received(1).HandleAsync(testEvent, Arg.Any<CancellationToken>());

        await app.StopAsync();
    }

    public sealed class TestEvent : IIntegrationEvent
    {
        public string Message { get; set; } = string.Empty;
        public Guid Id { get; } = Guid.NewGuid();
        public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    }
}