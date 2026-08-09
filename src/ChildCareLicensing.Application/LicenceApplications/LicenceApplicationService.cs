using ChildCareLicensing.Application.Abstractions;
using ChildCareLicensing.Domain.Enums;
using ChildCareLicensing.Domain.Licensing;

namespace ChildCareLicensing.Application.LicenceApplications;

public sealed class LicenceApplicationService(ILicenceApplicationRepository repository) : ILicenceApplicationService
{
    public async Task<Guid> CreateDraftAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        if (!await repository.FacilityExistsAsync(facilityId, cancellationToken))
        {
            throw new KeyNotFoundException($"Facility '{facilityId}' was not found.");
        }

        var openApplicationId = await repository.GetOpenApplicationIdForFacilityAsync(facilityId, cancellationToken);
        if (openApplicationId.HasValue)
        {
            return openApplicationId.Value;
        }

        return await repository.CreateDraftApplicationAsync(facilityId, cancellationToken);
    }

    public Task<LicenceApplicationDetails?> GetAsync(Guid applicationId, CancellationToken cancellationToken = default)
        => repository.GetApplicationDetailsAsync(applicationId, cancellationToken);

    public Task<Guid?> GetOwningOperatorIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
        => repository.GetOwningOperatorIdAsync(applicationId, cancellationToken);

    public async Task<FacilityCapacityValidationResult> ValidateAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await repository.GetApplicationDetailsAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        return BuildValidation(application);
    }

    public async Task<SubmitLicenceApplicationResult> SubmitAsync(
        Guid applicationId,
        string submittedBy,
        CancellationToken cancellationToken = default)
    {
        var application = await repository.GetApplicationDetailsAsync(applicationId, cancellationToken);
        if (application is null)
        {
            return new SubmitLicenceApplicationResult(
                false,
                "Application not found.",
                applicationId,
                string.Empty,
                null);
        }

        var currentStatus = Enum.Parse<ApplicationStatus>(application.Status);
        if (!ApplicationWorkflow.CanTransition(currentStatus, ApplicationStatus.Submitted))
        {
            return new SubmitLicenceApplicationResult(
                false,
                $"An application that is {ApplicationWorkflow.Describe(currentStatus).ToLowerInvariant()} cannot be submitted.",
                applicationId,
                application.Status,
                null);
        }

        if (application.Rooms.Count == 0)
        {
            return new SubmitLicenceApplicationResult(
                false,
                "At least one room is required before submitting an application.",
                applicationId,
                application.Status,
                null);
        }

        var validation = BuildValidation(application);
        if (!validation.IsValid)
        {
            return new SubmitLicenceApplicationResult(
                false,
                "Capacity validation failed. Fix the room details and try again.",
                applicationId,
                application.Status,
                validation);
        }

        var licensedCapacities = application.Rooms.ToDictionary(
            r => r.Id,
            r => validation.Rooms.First(v => v.RoomName == r.Name).LicensedCapacity);

        await repository.SubmitApplicationAsync(
            applicationId,
            DateTime.UtcNow,
            licensedCapacities,
            submittedBy,
            cancellationToken);

        return new SubmitLicenceApplicationResult(
            true,
            null,
            applicationId,
            ApplicationStatus.Submitted.ToString(),
            validation);
    }

    private static FacilityCapacityValidationResult BuildValidation(LicenceApplicationDetails application)
    {
        var requests = application.Rooms.Select(r => new RoomCapacityRequest(
            r.Name,
            Enum.Parse<AgeGroup>(r.AgeGroup),
            r.FloorAreaSqM,
            r.ProposedCapacity));

        return CapacityRulesEngine.ValidateFacility(requests);
    }
}
