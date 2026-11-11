namespace Shared.Application.Messaging;

/// <summary>
/// Queues an integration event to be published once the current unit of work is saved.
/// The event is stored in the same transaction as the data change, so it is never lost
/// and never published for a change that was rolled back. Delivery is at least once.
/// </summary>
public interface IOutbox
{
    void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class;
}
