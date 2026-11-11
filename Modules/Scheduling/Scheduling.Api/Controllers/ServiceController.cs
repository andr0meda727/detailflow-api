using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Application.Catalog;
using Scheduling.Application.Catalog.CreateService;
using Scheduling.Application.Catalog.DeleteService;
using Scheduling.Application.Catalog.GetServices;
using Scheduling.Application.Catalog.UpdateService;
using Shared.Api.Errors;

namespace Scheduling.Api.Controllers;

// Singular route kept for existing clients of the API
[ApiController]
[Route("api/service")]
[Authorize]
public class ServiceController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ServiceResponseDto>>> GetAll(
        [FromServices] GetServicesHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponseDto>> Create(
        ServiceRequest request, [FromServices] CreateServiceHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(request, cancellationToken);
    }

    /// <summary>
    /// Changes the name, category and prices. Left out, the category and prices stay as they are.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ServiceResponseDto>> Update(
        Guid id, ServiceRequest request, [FromServices] UpdateServiceHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, [FromServices] DeleteServiceHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }
}
