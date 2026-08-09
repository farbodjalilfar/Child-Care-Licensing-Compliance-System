using ChildCareLicensing.Application.Facilities;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Persistence.Repositories;

public sealed class FacilityQueryService(ApplicationDbContext dbContext) : IFacilityQueryService
{
    private const string Unlicensed = "Unlicensed";

    public async Task<IReadOnlyList<FacilitySummary>> ListAsync(
        Guid? operatorId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Facilities.AsQueryable();

        if (operatorId is { } id)
        {
            query = query.Where(f => f.OperatorId == id);
        }

        return await Project(query.OrderBy(f => f.Name)).ToListAsync(cancellationToken);
    }

    public async Task<FacilitySummary?> GetAsync(Guid facilityId, CancellationToken cancellationToken = default)
        => await Project(dbContext.Facilities.Where(f => f.Id == facilityId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid?> GetOwningOperatorIdAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Facilities
            .AsNoTracking()
            .Where(f => f.Id == facilityId)
            .Select(f => (Guid?)f.OperatorId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<FacilitySummary> Project(IQueryable<Domain.Entities.Facility> query)
    {
        return query
            .AsNoTracking()
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.City,
                f.Province,
                RoomCount = f.Rooms.Count,
                LicensedCapacity = f.Rooms.Sum(r => r.LicensedCapacity ?? 0),
                Licence = f.Licences
                    .OrderByDescending(l => l.IssuedAtUtc)
                    .Select(l => new { l.Status, l.ExpiresAtUtc })
                    .FirstOrDefault(),
                ApplicationStatus = f.Applications
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Select(a => (string?)a.Status.ToString())
                    .FirstOrDefault()
            })
            .Select(f => new FacilitySummary(
                f.Id,
                f.Name,
                f.City,
                f.Province,
                f.RoomCount,
                f.LicensedCapacity,
                f.Licence != null ? f.Licence.Status.ToString() : Unlicensed,
                f.Licence != null ? f.Licence.ExpiresAtUtc : null,
                f.ApplicationStatus));
    }
}
