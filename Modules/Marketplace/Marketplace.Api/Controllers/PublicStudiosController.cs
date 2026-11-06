using Marketplace.Application.Listings;
using Marketplace.Application.Search.GetPublicStudio;
using Marketplace.Application.Search.SearchStudios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Api.Errors;
using Shared.Api.RateLimiting;
using Shared.Api.Tenancy;
using Shared.Kernel.Pagination;

namespace Marketplace.Api.Controllers;

/// <summary>
/// The customer map: anyone can search published studios, without signing in. Signed-in callers (customers, or
/// staff of several studios) never need to choose a studio here, and requests are rate-limited per IP.
/// </summary>
[ApiController]
[Route("api/public/studios")]
[AllowAnonymous]
[AllowWithoutStudio]
[EnableRateLimiting(RateLimits.Public)]
public class PublicStudiosController : ControllerBase
{
    /// <summary>
    /// Published studios inside the visible map area, e.g.
    /// <c>?minLat=50.0&amp;maxLat=50.1&amp;minLng=19.8&amp;maxLng=20.1&amp;category=Interior&amp;vehicleSize=Large</c>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<StudioPinDto>>> Search(
        [FromQuery] StudioSearchRequest request, [FromQuery] PageRequest paging,
        [FromServices] SearchStudiosHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(request, paging, cancellationToken);
    }

    [HttpGet("{studioId:guid}")]
    public async Task<ActionResult<ListingDto>> Get(
        Guid studioId, [FromServices] GetPublicStudioHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(studioId, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }
}
