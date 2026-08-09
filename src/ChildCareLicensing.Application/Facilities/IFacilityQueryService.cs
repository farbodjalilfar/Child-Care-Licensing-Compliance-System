namespace ChildCareLicensing.Application.Facilities;

public sealed record FacilitySummary(
    Guid Id,
    string Name,
    string City,
    string Province,
    int RoomCount,
    int LicensedCapacity,
    string LicenceStatus,
    DateTime? LicenceExpiresAtUtc,
    string? ApplicationStatus);

public interface IFacilityQueryService
{
    /// <summary>Pass an operator id to return only that operator's centres.</summary>
    Task<IReadOnlyList<FacilitySummary>> ListAsync(
        Guid? operatorId = null,
        CancellationToken cancellationToken = default);

    Task<FacilitySummary?> GetAsync(Guid facilityId, CancellationToken cancellationToken = default);

    Task<Guid?> GetOwningOperatorIdAsync(Guid facilityId, CancellationToken cancellationToken = default);
}
