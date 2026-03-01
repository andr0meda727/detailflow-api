using Core.DTOs.Clients;

namespace Core.Interfaces.Services;

public interface IClientsService
{
    Task<List<ClientResponseDto>> GetAllClientsAsync(string? search);
    Task<ClientResponseDto?> GetClientByIdAsync(Guid id);
    Task<ClientResponseDto> AddClientAsync(CreateClientDto dto);
    Task<ClientResponseDto?> UpdateClientAsync(Guid id, CreateClientDto dto);
    Task<bool> DeleteClientAsync(Guid id);
}