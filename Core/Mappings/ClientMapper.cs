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
}