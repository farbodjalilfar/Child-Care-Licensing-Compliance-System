namespace ChildCareLicensing.Application.Facilities;

public sealed record FacilitySummary(
    Guid Id,
    string Name,
    string City,
    string Province,
    int RoomCount);

public interface IFacilityQueryService
{
    Task<IReadOnlyList<FacilitySummary>> ListAsync(CancellationToken cancellationToken = default);

    Task<FacilitySummary?> GetAsync(Guid facilityId, CancellationToken cancellationToken = default);
}
