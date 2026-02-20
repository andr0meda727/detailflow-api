using Core.DTOs.Clients;
using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services;

public class ClientsService(
    IClientRepository clientRepository,
    ITenantContext tenantContext,
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

    public async Task<ClientResponseDto> AddClientAsync(CreateClientDto dto)
    {
        var client = mapper.MapToEntity(dto);

        client.Id = Guid.NewGuid();
        client.StudioId = tenantContext.StudioId;
        client.CreatedAt = DateTime.UtcNow;
        client.UpdatedAt = DateTime.UtcNow;

        var addedClient = await clientRepository.AddAsync(client);

        return mapper.MapClientToResponse(addedClient);
    }
}