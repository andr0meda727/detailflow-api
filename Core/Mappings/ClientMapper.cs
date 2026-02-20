using System.Diagnostics.CodeAnalysis;
using Core.DTOs.Clients;
using Core.Interfaces.Mappings;
using Core.Models;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ClientMapper : IClientMapper
{
    public partial ClientResponseDto MapClientToResponse(Client client);

    [MapperIgnoreTarget(nameof(Client.Id))]
    [MapperIgnoreTarget(nameof(Client.StudioId))]
    [MapperIgnoreTarget(nameof(Client.CreatedAt))]
    [MapperIgnoreTarget(nameof(Client.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Client.Vehicles))]
    [MapperIgnoreTarget(nameof(Client.Studio))]
    [MapperIgnoreTarget(nameof(Client.Bookings))]
    [MapperIgnoreTarget(nameof(Client.IsDeleted))]
    public partial Client MapToEntity(CreateClientDto dto);

    [MapperIgnoreTarget(nameof(Client.Id))]
    [MapperIgnoreTarget(nameof(Client.StudioId))]
    [MapperIgnoreTarget(nameof(Client.CreatedAt))]
    [MapperIgnoreTarget(nameof(Client.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Client.Vehicles))]
    [MapperIgnoreTarget(nameof(Client.Bookings))]
    [MapperIgnoreTarget(nameof(Client.Studio))]
    [MapperIgnoreTarget(nameof(Client.IsDeleted))]
    public partial void UpdateClient(CreateClientDto dto, Client client);
}