using Core.DTOs.Clients;
using Core.Interfaces;
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

    public async Task<ClientResponseDto> AddClientAsync(CreateClientDto dto)
    {
        var client = mapper.MapToEntity(dto);

        var addedClient = await clientRepository.AddAsync(client);

        return mapper.MapClientToResponse(addedClient);
    }

    public async Task<ClientResponseDto?> UpdateClientAsync(Guid id, CreateClientDto dto)
    {
        var client = await clientRepository.GetByIdAsync(id);

        if (client == null) return null;

        mapper.UpdateClient(dto, client);

        await clientRepository.UpdateAsync(client);

        return await GetClientByIdAsync(client.Id);
    }

    public async Task<bool> DeleteClientAsync(Guid id)
    {
        var client = await clientRepository.GetByIdAsync(id);

        if (client == null) return false;

        client.IsDeleted = true;

        await clientRepository.UpdateAsync(client);
        return true;
    }
}