namespace ChildCareLicensing.Application.Abstractions;

public interface ILicenceApplicationRepository
{
    Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken = default);

    Task<Guid?> GetDraftApplicationIdForFacilityAsync(Guid facilityId, CancellationToken cancellationToken = default);

    Task<LicenceApplicationDetails?> GetApplicationDetailsAsync(Guid applicationId, CancellationToken cancellationToken = default);

    Task<Guid> CreateDraftApplicationAsync(Guid facilityId, CancellationToken cancellationToken = default);

    Task SubmitApplicationAsync(
        Guid applicationId,
        DateTime submittedAtUtc,
        IReadOnlyDictionary<Guid, int> licensedCapacitiesByRoomId,
        CancellationToken cancellationToken = default);
}

public sealed record LicenceApplicationRoomDetails(
    Guid Id,
    string Name,
    string AgeGroup,
    decimal FloorAreaSqM,
    int ProposedCapacity,
    int? LicensedCapacity);

public sealed record LicenceApplicationDetails(
    Guid Id,
    Guid FacilityId,
    string FacilityName,
    string Status,
    DateTime? SubmittedAtUtc,
    IReadOnlyList<LicenceApplicationRoomDetails> Rooms);
