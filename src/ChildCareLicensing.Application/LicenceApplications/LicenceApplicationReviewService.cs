using ChildCareLicensing.Domain.Enums;
using ChildCareLicensing.Domain.Licensing;

namespace ChildCareLicensing.Application.LicenceApplications;

public sealed class LicenceApplicationReviewService(ILicenceApplicationReviewRepository repository)
    : ILicenceApplicationReviewService
{
    private const int LicenceTermYears = 1;

    public Task<IReadOnlyList<ReviewQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default)
        => repository.GetQueueAsync(cancellationToken);

    public Task<IReadOnlyList<ApplicationHistoryEntry>> GetHistoryAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
        => repository.GetHistoryAsync(applicationId, cancellationToken);

    public Task<ReviewDecisionResult> StartReviewAsync(
        Guid applicationId,
        string reviewer,
        CancellationToken cancellationToken = default)
        => TransitionAsync(
            applicationId,
            ApplicationStatus.UnderReview,
            reviewer,
            "Review started.",
            cancellationToken);

    public Task<ReviewDecisionResult> RequestMoreInformationAsync(
        Guid applicationId,
        string reviewer,
        string notes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return Task.FromResult(new ReviewDecisionResult(
                false,
                "Explain what additional information is needed.",
                string.Empty));
        }

        return TransitionAsync(
            applicationId,
            ApplicationStatus.AdditionalInfoRequired,
            reviewer,
            notes,
            cancellationToken);
    }

    public Task<ReviewDecisionResult> RejectAsync(
        Guid applicationId,
        string reviewer,
        string notes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return Task.FromResult(new ReviewDecisionResult(
                false,
                "A reason is required when rejecting an application.",
                string.Empty));
        }

        return TransitionAsync(
            applicationId,
            ApplicationStatus.Rejected,
            reviewer,
            notes,
            cancellationToken);
    }

    public async Task<ReviewDecisionResult> ApproveAsync(
        Guid applicationId,
        string reviewer,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var current = await repository.GetStatusAsync(applicationId, cancellationToken);
        if (current is null)
        {
            return new ReviewDecisionResult(false, "Application not found.", string.Empty);
        }

        if (!ApplicationWorkflow.CanTransition(current.Value, ApplicationStatus.Approved))
        {
            return new ReviewDecisionResult(false, RefusalMessage(current.Value), current.Value.ToString());
        }

        var decidedAtUtc = DateTime.UtcNow;

        var licenceNumber = await repository.ApproveAndIssueLicenceAsync(
            applicationId,
            reviewer,
            notes,
            decidedAtUtc,
            decidedAtUtc.Date.AddYears(LicenceTermYears),
            cancellationToken);

        return new ReviewDecisionResult(
            true,
            null,
            ApplicationStatus.Approved.ToString(),
            licenceNumber);
    }

    private async Task<ReviewDecisionResult> TransitionAsync(
        Guid applicationId,
        ApplicationStatus target,
        string reviewer,
        string? notes,
        CancellationToken cancellationToken)
    {
        var current = await repository.GetStatusAsync(applicationId, cancellationToken);
        if (current is null)
        {
            return new ReviewDecisionResult(false, "Application not found.", string.Empty);
        }

        if (!ApplicationWorkflow.CanTransition(current.Value, target))
        {
            return new ReviewDecisionResult(false, RefusalMessage(current.Value), current.Value.ToString());
        }

        await repository.RecordTransitionAsync(
            applicationId,
            current.Value,
            target,
            reviewer,
            notes,
            DateTime.UtcNow,
            cancellationToken);

        return new ReviewDecisionResult(true, null, target.ToString());
    }

    private static string RefusalMessage(ApplicationStatus current)
        => ApplicationWorkflow.IsClosed(current)
            ? $"This application was already {ApplicationWorkflow.Describe(current).ToLowerInvariant()} and cannot be changed."
            : $"That action is not available while the application is {ApplicationWorkflow.Describe(current).ToLowerInvariant()}.";
}
