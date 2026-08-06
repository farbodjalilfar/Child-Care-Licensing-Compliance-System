using ChildCareLicensing.Application.Abstractions;
using ChildCareLicensing.Domain.Licensing;

namespace ChildCareLicensing.Application.LicenceApplications;

public sealed record SubmitLicenceApplicationResult(
    bool Succeeded,
    string? ErrorMessage,
    Guid ApplicationId,
    string Status,
    FacilityCapacityValidationResult? Validation);

public interface ILicenceApplicationService
{
    Task<Guid> CreateDraftAsync(Guid facilityId, CancellationToken cancellationToken = default);

    Task<LicenceApplicationDetails?> GetAsync(Guid applicationId, CancellationToken cancellationToken = default);

    Task<FacilityCapacityValidationResult> ValidateAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<SubmitLicenceApplicationResult> SubmitAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}
