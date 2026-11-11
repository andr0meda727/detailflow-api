using Crm.Contracts;
using Crm.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Tenancy;

namespace IntegrationTests.Messaging;

/// <summary>
/// Runs the dispatcher by hand against the CRM outbox. Other tests may leave messages behind,
/// so assertions only look at the messages created here.
/// </summary>
[TestFixture]
public class OutboxDispatcherTests
{
    private ServiceProvider _services = null!;
    private IBus _bus = null!;
    private OutboxDispatcher<CrmDbContext> _dispatcher = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection()
            .AddScoped<ITenantContext, TenantContext>()
            .AddScoped(sp => PostgresFixture.CreateCrmDbContext(sp.GetRequiredService<ITenantContext>()))
            .BuildServiceProvider();
        _bus = Substitute.For<IBus>();
        _dispatcher = new OutboxDispatcher<CrmDbContext>(
            _services.GetRequiredService<IServiceScopeFactory>(), _bus, TimeProvider.System,
            NullLogger<OutboxDispatcher<CrmDbContext>>.Instance);
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        _dispatcher.Dispose();
        await _services.DisposeAsync();
    }

    private static async Task<Guid> EnqueueAsync(ClientDetailsChanged integrationEvent)
    {
        await using var db = PostgresFixture.CreateCrmDbContext(new TenantContext());
        db.Enqueue(integrationEvent);
        await db.SaveChangesAsync();
        return db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Id;
    }

    private static async Task<OutboxMessage> LoadAsync(Guid id)
    {
        await using var db = PostgresFixture.CreateCrmDbContext(new TenantContext());
        return await db.Set<OutboxMessage>().AsNoTracking().SingleAsync(m => m.Id == id);
    }

    // Leftovers from other tests may be ahead in the queue, so dispatch until ours has been handled
    private async Task<OutboxMessage> DispatchUntilHandledAsync(Guid id)
    {
        for (var round = 0; round < 20; round++)
        {
            await _dispatcher.DispatchBatchAsync(CancellationToken.None);
            var message = await LoadAsync(id);
            if (message.ProcessedAt != null || message.Attempts > 0) return message;
        }

        return await LoadAsync(id);
    }

    [Test]
    public async Task SavedEvent_IsPublishedAndMarkedProcessed()
    {
        var integrationEvent = new ClientDetailsChanged(Guid.NewGuid(), Guid.NewGuid(), "Jan", "500600700", null);
        var id = await EnqueueAsync(integrationEvent);

        var message = await DispatchUntilHandledAsync(id);

        Assert.That(message.ProcessedAt, Is.Not.Null);
        await _bus.Received(1).Publish(integrationEvent, typeof(ClientDetailsChanged), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailedPublish_IsCountedAndRetriedLater()
    {
        var integrationEvent = new ClientDetailsChanged(Guid.NewGuid(), Guid.NewGuid(), "Awaria", "500600700", null);
        _bus.Publish(integrationEvent, typeof(ClientDetailsChanged), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("broker down"));
        var id = await EnqueueAsync(integrationEvent);

        var message = await DispatchUntilHandledAsync(id);

        Assert.That(message.ProcessedAt, Is.Null);
        Assert.That(message.Attempts, Is.EqualTo(1));
        Assert.That(message.LastError, Is.EqualTo("broker down"));
    }
}
