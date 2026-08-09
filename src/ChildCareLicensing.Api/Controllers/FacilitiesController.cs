using ChildCareLicensing.Application.Facilities;
using Microsoft.AspNetCore.Mvc;

namespace ChildCareLicensing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FacilitiesController(IFacilityQueryService facilities) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FacilitySummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await facilities.ListAsync(cancellationToken));

    [HttpGet("{facilityId:guid}")]
    [ProducesResponseType<FacilitySummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid facilityId, CancellationToken cancellationToken)
    {
        var facility = await facilities.GetAsync(facilityId, cancellationToken);
        return facility is null ? NotFound() : Ok(facility);
    }
}
