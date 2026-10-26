using Api.Extensions;
using Core.DTOs.Auth;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IOwnerRegistrationService registrationService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterOwnerDto dto)
    {
        var result = await registrationService.RegisterAsync(dto);

        if (!result.Success)
            return this.ToProblem(result.Error!);

        return Ok(new { message = "Konto zostało utworzone. Możesz się zalogować." });
    }
}