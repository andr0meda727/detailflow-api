using Core.DTOs.Clients;
using Core.Models;

namespace Core.Interfaces.Mappings;

public interface IClientMapper
{
    public ClientResponseDto MapClientToResponse(Client client);
    public Client MapToEntity(CreateClientDto dto);
    public void UpdateClient(CreateClientDto dto, Client client);
}