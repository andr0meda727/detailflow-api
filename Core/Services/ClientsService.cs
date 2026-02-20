using Core.DTOs.Clients;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services;

public class ClientsService(
    IClientRepository clientRepository,
    IClientMapper mapper) : IClientsService
{
    public async Task<List<ClientResponseDto>> GetAllClientsAsync(string? search)
    {
        var clients = await clientRepository.GetAllWithVehiclesAsync(search);

        return clients.Select(mapper.MapClientToResponse).ToList();
    }

    public async Task<ClientResponseDto?> GetClientByIdAsync(Guid id)
    {
        var client = await clientRepository.GetClientByIdWithVehiclesAsync(id);

        return client == null ? null : mapper.MapClientToResponse(client);
    }
}