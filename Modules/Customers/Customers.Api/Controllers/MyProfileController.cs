using Customers.Application;
using Customers.Application.Profile.GetMyProfile;
using Customers.Application.Profile.SaveMyProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;
using Shared.Api.Tenancy;

namespace Customers.Api.Controllers;

/// <summary>
/// The signed-in customer's contact details. Works for any account, without choosing a studio.
/// </summary>
[ApiController]
[Route("api/me/profile")]
[Authorize]
[AllowWithoutStudio]
public class MyProfileController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(
        [FromServices] GetMyProfileHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Save(
        ProfileRequest request, [FromServices] SaveMyProfileHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(request, cancellationToken);
    }
}
