using ChildCareLicensing.Application.LicenceApplications;
using Microsoft.AspNetCore.Mvc;

namespace ChildCareLicensing.Api.Controllers;

[ApiController]
[Route("api/licence-applications")]
public class LicenceApplicationsController(ILicenceApplicationService service) : ControllerBase
{
    [HttpGet("{applicationId:guid}")]
    public async Task<IActionResult> GetById(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await service.GetAsync(applicationId, cancellationToken);
        return application is null ? NotFound() : Ok(application);
    }

    [HttpGet("{applicationId:guid}/validation")]
    public async Task<IActionResult> Validate(Guid applicationId, CancellationToken cancellationToken)
    {
        try
        {
            var validation = await service.ValidateAsync(applicationId, cancellationToken);
            return Ok(validation);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{applicationId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await service.SubmitAsync(applicationId, cancellationToken);

        if (result.ErrorMessage == "Application not found.")
        {
            return NotFound(result);
        }

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

[ApiController]
[Route("api/facilities/{facilityId:guid}/licence-applications")]
public class FacilityLicenceApplicationsController(ILicenceApplicationService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateDraft(Guid facilityId, CancellationToken cancellationToken)
    {
        try
        {
            var applicationId = await service.CreateDraftAsync(facilityId, cancellationToken);
            return CreatedAtAction(
                nameof(LicenceApplicationsController.GetById),
                "LicenceApplications",
                new { applicationId },
                new { id = applicationId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
