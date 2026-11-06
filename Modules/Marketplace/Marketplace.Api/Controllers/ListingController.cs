using Marketplace.Application.Listings;
using Marketplace.Application.Listings.GetMyListing;
using Marketplace.Application.Listings.PublishListing;
using Marketplace.Application.Listings.SaveListing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Authorization;
using Shared.Api.Errors;

namespace Marketplace.Api.Controllers;

/// <summary>
/// The current studio's public profile on the customer map. Staff can see it; only the owner changes it.
/// </summary>
[ApiController]
[Route("api/marketplace/listing")]
[Authorize]
public class ListingController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ListingDto>> Get(
        [FromServices] GetMyListingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    /// <summary>
    /// Creates the profile (unpublished, with the studio's current services) or updates it.
    /// </summary>
    [HttpPut]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult<ListingDto>> Save(
        ListingRequest request, [FromServices] SaveListingHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(request, cancellationToken);
    }

    [HttpPost("publish")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<IActionResult> Publish(
        [FromServices] PublishListingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(publish: true, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }

    [HttpPost("unpublish")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<IActionResult> Unpublish(
        [FromServices] PublishListingHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(publish: false, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }
}
