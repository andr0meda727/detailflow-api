using Core.DTOs.Clients;
using Shared.Kernel.Pagination;

namespace Core.Interfaces.Services;

public interface IClientsService
{
    Task<PagedResponse<ClientResponseDto>> GetAllClientsAsync(PageRequest page, string? search);
    Task<ClientResponseDto?> GetClientByIdAsync(Guid id);
    Task<ClientResponseDto> AddClientAsync(CreateClientDto dto);
    Task<ClientResponseDto?> UpdateClientAsync(Guid id, CreateClientDto dto);
    Task<bool> DeleteClientAsync(Guid id);
}