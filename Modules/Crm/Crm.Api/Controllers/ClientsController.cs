using Crm.Application.Clients;
using Crm.Application.Clients.CreateClient;
using Crm.Application.Clients.DeleteClient;
using Crm.Application.Clients.GetClient;
using Crm.Application.Clients.GetClients;
using Crm.Application.Clients.UpdateClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;
using Shared.Kernel.Pagination;

namespace Crm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ClientResponseDto>>> GetAll(
        [FromQuery] PageRequest paging, [FromQuery] string? search,
        [FromServices] GetClientsHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(paging, search, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponseDto>> GetById(
        Guid id, [FromServices] GetClientHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponseDto>> Create(
        ClientRequest request, [FromServices] CreateClientHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : this.ToProblem(result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientResponseDto>> Update(
        Guid id, ClientRequest request, [FromServices] UpdateClientHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, [FromServices] DeleteClientHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }
}
