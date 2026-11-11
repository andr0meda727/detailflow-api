using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Crm.Application.Clients.DeleteClient;

public sealed class DeleteClientHandler(ICrmDbContext db)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client == null) return CrmErrors.ClientNotFound(id);

        client.Delete();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
