namespace ChildCareLicensing.Application.PublicRegistry;

/// <summary>
/// Read-only view of the licence register, intended for anonymous public access.
/// Deliberately excludes operator contact details and reviewer notes.
/// </summary>
public interface IPublicRegistryService
{
    Task<IReadOnlyList<PublicFacilityListing>> SearchAsync(
        string? city,
        string? name,
        CancellationToken cancellationToken = default);

    Task<PublicFacilityDetail?> GetAsync(Guid facilityId, CancellationToken cancellationToken = default);
}

public sealed record PublicFacilityListing(
    Guid FacilityId,
    string Name,
    string City,
    string Province,
    string? LicenceNumber,
    string LicenceStatus,
    DateTime? LicenceExpiresAtUtc,
    int OpenViolations);

public sealed record PublicInspectionSummary(
    DateTime InspectionDateUtc,
    string? Summary,
    int ViolationCount,
    int CriticalViolationCount);

public sealed record PublicFacilityDetail(
    Guid FacilityId,
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Province,
    string PostalCode,
    string? LicenceNumber,
    string LicenceStatus,
    DateTime? LicenceIssuedAtUtc,
    DateTime? LicenceExpiresAtUtc,
    int LicensedCapacity,
    IReadOnlyList<PublicInspectionSummary> Inspections);
