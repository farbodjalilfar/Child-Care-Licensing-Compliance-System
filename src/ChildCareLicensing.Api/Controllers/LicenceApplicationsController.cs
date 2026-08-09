using ChildCareLicensing.Api.Security;
using ChildCareLicensing.Application.Facilities;
using ChildCareLicensing.Application.LicenceApplications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChildCareLicensing.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/licence-applications")]
public class LicenceApplicationsController(ILicenceApplicationService service) : ControllerBase
{
    [HttpGet("{applicationId:guid}")]
    public async Task<IActionResult> GetById(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await service.GetAsync(applicationId, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        return await IsForbiddenAsync(applicationId, cancellationToken)
            ? Forbid()
            : Ok(application);
    }

    [HttpGet("{applicationId:guid}/validation")]
    public async Task<IActionResult> Validate(Guid applicationId, CancellationToken cancellationToken)
    {
        try
        {
            if (await IsForbiddenAsync(applicationId, cancellationToken))
            {
                return Forbid();
            }

            return Ok(await service.ValidateAsync(applicationId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{applicationId:guid}/submit")]
    [Authorize(Policy = AuthorizationPolicies.Operator)]
    public async Task<IActionResult> Submit(Guid applicationId, CancellationToken cancellationToken)
    {
        if (await IsForbiddenAsync(applicationId, cancellationToken))
        {
            return Forbid();
        }

        var result = await service.SubmitAsync(applicationId, User.SignInName(), cancellationToken);

        if (result.ErrorMessage == "Application not found.")
        {
            return NotFound(result);
        }

        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    private async Task<bool> IsForbiddenAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        if (User.OperatorId() is not { } operatorId)
        {
            return false;
        }

        var owner = await service.GetOwningOperatorIdAsync(applicationId, cancellationToken);
        return owner is not null && owner != operatorId;
    }
}

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Operator)]
[Route("api/facilities/{facilityId:guid}/licence-applications")]
public class FacilityLicenceApplicationsController(
    ILicenceApplicationService service,
    IFacilityQueryService facilities) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateDraft(Guid facilityId, CancellationToken cancellationToken)
    {
        var owner = await facilities.GetOwningOperatorIdAsync(facilityId, cancellationToken);
        if (owner is null)
        {
            return NotFound();
        }

        if (User.OperatorId() != owner)
        {
            return Forbid();
        }

        var applicationId = await service.CreateDraftAsync(facilityId, cancellationToken);

        return CreatedAtAction(
            nameof(LicenceApplicationsController.GetById),
            "LicenceApplications",
            new { applicationId },
            new { id = applicationId });
    }
}

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Reviewer)]
[Route("api/review/licence-applications")]
public class LicenceApplicationReviewController(ILicenceApplicationReviewService review) : ControllerBase
{
    public sealed record DecisionRequest(string? Notes);

    [HttpGet("queue")]
    public async Task<IActionResult> Queue(CancellationToken cancellationToken)
        => Ok(await review.GetQueueAsync(cancellationToken));

    [HttpGet("{applicationId:guid}/history")]
    public async Task<IActionResult> History(Guid applicationId, CancellationToken cancellationToken)
        => Ok(await review.GetHistoryAsync(applicationId, cancellationToken));

    [HttpPost("{applicationId:guid}/start-review")]
    public async Task<IActionResult> StartReview(Guid applicationId, CancellationToken cancellationToken)
        => Respond(await review.StartReviewAsync(applicationId, User.SignInName(), cancellationToken));

    [HttpPost("{applicationId:guid}/request-information")]
    public async Task<IActionResult> RequestInformation(
        Guid applicationId,
        DecisionRequest request,
        CancellationToken cancellationToken)
        => Respond(await review.RequestMoreInformationAsync(
            applicationId, User.SignInName(), request.Notes ?? string.Empty, cancellationToken));

    [HttpPost("{applicationId:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid applicationId,
        DecisionRequest request,
        CancellationToken cancellationToken)
        => Respond(await review.ApproveAsync(applicationId, User.SignInName(), request.Notes, cancellationToken));

    [HttpPost("{applicationId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid applicationId,
        DecisionRequest request,
        CancellationToken cancellationToken)
        => Respond(await review.RejectAsync(
            applicationId, User.SignInName(), request.Notes ?? string.Empty, cancellationToken));

    private IActionResult Respond(ReviewDecisionResult result)
    {
        if (result.Succeeded)
        {
            return Ok(result);
        }

        return result.ErrorMessage == "Application not found."
            ? NotFound(result)
            : BadRequest(result);
    }
}
