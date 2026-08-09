using ChildCareLicensing.Api.Security;
using ChildCareLicensing.Application.Facilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChildCareLicensing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/facilities")]
public class FacilitiesController(IFacilityQueryService facilities) : ControllerBase
{
    /// <summary>
    /// Operators see only their own centres; ministry staff see the whole register.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await facilities.ListAsync(User.OperatorId(), cancellationToken));

    [HttpGet("{facilityId:guid}")]
    public async Task<IActionResult> GetById(Guid facilityId, CancellationToken cancellationToken)
    {
        var facility = await facilities.GetAsync(facilityId, cancellationToken);
        if (facility is null)
        {
            return NotFound();
        }

        if (User.OperatorId() is { } operatorId)
        {
            var owner = await facilities.GetOwningOperatorIdAsync(facilityId, cancellationToken);
            if (owner != operatorId)
            {
                return Forbid();
            }
        }

        return Ok(facility);
    }
}
