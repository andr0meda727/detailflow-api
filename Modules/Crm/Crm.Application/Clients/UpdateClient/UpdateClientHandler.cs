using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Crm.Application.Clients.UpdateClient;

public sealed class UpdateClientHandler(ICrmDbContext db)
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

        return ClientResponseDto.From(client);
    }
}
