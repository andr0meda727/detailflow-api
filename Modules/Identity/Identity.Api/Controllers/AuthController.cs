using Identity.Application;
using Identity.Application.Studios.RegisterOwner;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterOwnerRequest request, [FromServices] RegisterOwnerHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Success
            ? Ok(new { message = "Konto zostało utworzone. Możesz się zalogować." })
            : this.ToProblem(result.Error!);
    }
}
