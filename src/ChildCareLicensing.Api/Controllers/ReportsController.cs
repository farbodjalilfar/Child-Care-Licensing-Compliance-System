using ChildCareLicensing.Application.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace ChildCareLicensing.Api.Controllers;

/// <summary>
/// Compliance reports for ministry staff. Backed by stored procedures via Dapper.
/// </summary>
[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsController(IComplianceReportService reports) : ControllerBase
{
    [HttpGet("violations-by-category")]
    [ProducesResponseType<IReadOnlyList<ViolationsByCategoryRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ViolationsByCategory(
        [FromQuery] int lookbackDays = 365,
        CancellationToken cancellationToken = default)
    {
        if (lookbackDays is < 1 or > 3650)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid lookback window.",
                Detail = "lookbackDays must be between 1 and 3650."
            });
        }

        var rows = await reports.GetViolationsByCategoryAsync(lookbackDays, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("facility-compliance")]
    [ProducesResponseType<IReadOnlyList<FacilityComplianceRow>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> FacilityCompliance(CancellationToken cancellationToken)
    {
        var rows = await reports.GetFacilityComplianceAsync(cancellationToken);
        return Ok(rows);
    }
}
