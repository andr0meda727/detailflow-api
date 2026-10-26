using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Crm.Application.Clients.GetClient;

public sealed class GetClientHandler(ICrmDbContext db)
{
    public async Task<Result<ClientResponseDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await db.Clients
            .Include(c => c.Vehicles)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return client == null ? CrmErrors.ClientNotFound(id) : ClientResponseDto.From(client);
    }
}
