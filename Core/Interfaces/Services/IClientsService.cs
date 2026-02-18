using Core.DTOs.Clients;

namespace Core.Interfaces.Services;

public interface IClientsService
{
    Task<List<ClientResponseDto>> GetAllClientsAsync(string? search);
    Task<ClientResponseDto?> GetClientByIdAsync(Guid id);
}