using Shared.Api.Errors;
using Shared.Kernel.Pagination;
using Shared.Kernel.Results;
using Core.DTOs.Clients;
using Core.Interfaces.Services;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController(IClientsService clientsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ClientResponseDto>>> GetAll(
        [FromQuery] PageRequest paging, [FromQuery] string? search)
    {
        var clients = await clientsService.GetAllClientsAsync(paging, search);
        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponseDto>> GetById(Guid id)
    {
        var client = await clientsService.GetClientByIdAsync(id);

        if (client == null)
        {
            return this.ToProblem(Error.NotFound("CLIENT_NOT_FOUND", $"Client {id} not found"));
        }

        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponseDto>> Create([FromBody] CreateClientDto dto)
    {
        var result = await clientsService.AddClientAsync(dto);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientResponseDto>> Update(Guid id, [FromBody] CreateClientDto dto)
    {
        var result = await clientsService.UpdateClientAsync(id, dto);

        if (result == null)
        {
            return this.ToProblem(Error.NotFound("CLIENT_NOT_FOUND", $"Client {id} not found"));
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await clientsService.DeleteClientAsync(id);

        if (!deleted) return this.ToProblem(Error.NotFound("CLIENT_NOT_FOUND", $"Client {id} not found"));

        return NoContent();
    }
}