using ChildCareLicensing.Application.PublicRegistry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChildCareLicensing.Api.Controllers;

/// <summary>
/// Anonymous endpoints backing the public "find a licensed child care centre" lookup.
/// Responses are cached because the register changes at most daily.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/facilities")]
[Produces("application/json")]
public class PublicRegistryController(IPublicRegistryService registry) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = ["city", "name"])]
    [ProducesResponseType<IReadOnlyList<PublicFacilityListing>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? city,
        [FromQuery] string? name,
        CancellationToken cancellationToken)
    {
        var results = await registry.SearchAsync(city, name, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{facilityId:guid}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType<PublicFacilityDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid facilityId, CancellationToken cancellationToken)
    {
        var facility = await registry.GetAsync(facilityId, cancellationToken);
        return facility is null ? NotFound() : Ok(facility);
    }
}
