using Core.DTOs.Clients;
using Shared.Kernel.Tenancy;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Shared.Kernel.Pagination;

namespace Core.Services;

public class ClientsService(
    IClientRepository clientRepository,
    IClientMapper mapper,
    IVehicleRepository vehicleRepository,
    IVehicleMapper vehicleMapper) : IClientsService
{
    public async Task<PagedResponse<ClientResponseDto>> GetAllClientsAsync(PageRequest page, string? search)
    {
        var clients = await clientRepository.GetAllWithVehiclesAsync(page, search);

        return clients.Map(mapper.MapClientToResponse);
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

        var addedClient = await clientRepository.AddAsync(client);

        if (dto.Vehicle != null)
        {
            var vehicleEntity = vehicleMapper.MapToEntity(dto.Vehicle);

            vehicleEntity.ClientId = addedClient.Id;

            await vehicleRepository.AddAsync(vehicleEntity);
        }

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