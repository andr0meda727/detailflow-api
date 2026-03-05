using Core.DTOs.Services;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceController(IServicesService servicesService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ServiceResponseDto>>> GetAll()
    {
        var services = await servicesService.GetAllAsync();
        return Ok(services);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponseDto>> Create([FromBody] CreateServiceDto serviceData)
    {
        var result = await servicesService.AddAsync(serviceData);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await servicesService.DeleteAsync(id);

        if (!deleted) return NotFound($"Service {id} doesn't exist");

        return NoContent();
    }
}
