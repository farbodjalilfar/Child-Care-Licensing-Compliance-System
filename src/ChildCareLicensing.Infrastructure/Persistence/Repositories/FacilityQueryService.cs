using ChildCareLicensing.Application.Facilities;
using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Persistence.Repositories;

public sealed class FacilityQueryService(ApplicationDbContext dbContext) : IFacilityQueryService
{
    public async Task<IReadOnlyList<FacilitySummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Facilities
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new FacilitySummary(
                f.Id,
                f.Name,
                f.City,
                f.Province,
                f.Rooms.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<FacilitySummary?> GetAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Facilities
            .AsNoTracking()
            .Where(f => f.Id == facilityId)
            .Select(f => new FacilitySummary(
                f.Id,
                f.Name,
                f.City,
                f.Province,
                f.Rooms.Count))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
