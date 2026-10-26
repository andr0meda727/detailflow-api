using Crm.Application.Abstractions;
using Crm.Domain;
using Shared.Kernel.Results;

namespace Crm.Application.Clients.CreateClient;

public sealed class CreateClientHandler(ICrmDbContext db)
{
    public async Task<Result<ClientResponseDto>> HandleAsync(ClientRequest request, CancellationToken cancellationToken)
    {
        if (await db.IsTakenAsync(request.Phone, null, cancellationToken)) return CrmErrors.PhoneTaken(request.Phone);

        var client = Client.Create(request.Name, request.Phone, request.Email, request.Nip);
        if (request.Vehicle is { } vehicle)
        {
            client.AddVehicle(vehicle.LicensePlate, vehicle.Brand, vehicle.Model);
        }

        // Client and vehicle are saved together
        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken);

        return ClientResponseDto.From(client);
    }
}
