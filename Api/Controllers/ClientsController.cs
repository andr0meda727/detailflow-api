using Core.DTOs.Clients;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController(IClientsService clientsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClientResponseDto>>> GetAll([FromQuery] string? search)
    {
        var clients = await clientsService.GetAllClientsAsync(search);
        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponseDto>> GetById(Guid id)
    {
        var client = await clientsService.GetClientByIdAsync(id);

        if (client == null)
        {
            return NotFound($"Client {id} doesn't exist");
        }

        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponseDto>> Create([FromBody] CreateClientDto dto)
    {
        var result = await clientsService.AddClientAsync(dto);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}