using Crm.Application.Abstractions;
using Crm.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Crm.Application.Clients.UpdateClient;

public sealed class UpdateClientHandler(ICrmDbContext db, IPublishEndpoint publisher)
{
    public async Task<Result<ClientResponseDto>> HandleAsync(
        Guid id, ClientRequest request, CancellationToken cancellationToken)
    {
        var client = await db.Clients.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client == null) return CrmErrors.ClientNotFound(id);

        if (await db.IsTakenAsync(request.Phone, id, cancellationToken)) return CrmErrors.PhoneTaken(request.Phone);

        // Vehicles are managed through their own endpoints, so request.Vehicle is ignored here
        client.UpdateDetails(request.Name, request.Phone, request.Email, request.Nip);
        await db.SaveChangesAsync(cancellationToken);

        // Other modules keep copies of these details (bookings, for example)
        await publisher.Publish(
            new ClientDetailsChanged(client.StudioId, client.Id, client.Name, client.Phone, client.Email), cancellationToken);

        return ClientResponseDto.From(client);
    }
}
