using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Application.LicenceApplications;

public sealed record ReviewQueueItem(
    Guid ApplicationId,
    Guid FacilityId,
    string FacilityName,
    string City,
    string OperatorName,
    string Status,
    DateTime? SubmittedAtUtc,
    int RoomCount,
    int RequestedCapacity);

public sealed record ApplicationHistoryEntry(
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAtUtc,
    string ChangedBy,
    string? Notes);

public sealed record ReviewDecisionResult(
    bool Succeeded,
    string? ErrorMessage,
    string Status,
    string? LicenceNumber = null);

public interface ILicenceApplicationReviewService
{
    Task<IReadOnlyList<ReviewQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationHistoryEntry>> GetHistoryAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<ReviewDecisionResult> StartReviewAsync(
        Guid applicationId,
        string reviewer,
        CancellationToken cancellationToken = default);

    Task<ReviewDecisionResult> RequestMoreInformationAsync(
        Guid applicationId,
        string reviewer,
        string notes,
        CancellationToken cancellationToken = default);

    Task<ReviewDecisionResult> ApproveAsync(
        Guid applicationId,
        string reviewer,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<ReviewDecisionResult> RejectAsync(
        Guid applicationId,
        string reviewer,
        string notes,
        CancellationToken cancellationToken = default);
}

/// <summary>Persistence operations the review workflow needs.</summary>
public interface ILicenceApplicationReviewRepository
{
    Task<IReadOnlyList<ReviewQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationHistoryEntry>> GetHistoryAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<ApplicationStatus?> GetStatusAsync(Guid applicationId, CancellationToken cancellationToken = default);

    Task RecordTransitionAsync(
        Guid applicationId,
        ApplicationStatus from,
        ApplicationStatus to,
        string changedBy,
        string? notes,
        DateTime changedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Approves the application and issues a licence in one transaction.</summary>
    Task<string> ApproveAndIssueLicenceAsync(
        Guid applicationId,
        string reviewer,
        string? notes,
        DateTime decidedAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}
