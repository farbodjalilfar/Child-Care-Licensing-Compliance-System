namespace ChildCareLicensing.Application.Abstractions;

public interface ILicenceApplicationRepository
{
    Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The application a facility is currently working through, whatever stage it has
    /// reached. Returns null only when every previous application has been decided.
    /// </summary>
    Task<Guid?> GetOpenApplicationIdForFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetOwningOperatorIdAsync(Guid applicationId, CancellationToken cancellationToken = default);

    Task<LicenceApplicationDetails?> GetApplicationDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateDraftApplicationAsync(Guid facilityId, CancellationToken cancellationToken = default);

    Task SubmitApplicationAsync(
        Guid applicationId,
        DateTime submittedAtUtc,
        IReadOnlyDictionary<Guid, int> licensedCapacitiesByRoomId,
        string submittedBy,
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
    string? ReviewerNotes,
    IReadOnlyList<LicenceApplicationRoomDetails> Rooms);
