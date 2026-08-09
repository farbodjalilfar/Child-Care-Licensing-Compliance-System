using ChildCareLicensing.Application.PublicRegistry;
using ChildCareLicensing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Persistence.Repositories;

public sealed class PublicRegistryService(ApplicationDbContext dbContext) : IPublicRegistryService
{
    private const string Unlicensed = "Unlicensed";

    public async Task<IReadOnlyList<PublicFacilityListing>> SearchAsync(
        string? city,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Facilities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(f => EF.Functions.Like(f.City, $"%{city.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(f => EF.Functions.Like(f.Name, $"%{name.Trim()}%"));
        }

        return await query
            .OrderBy(f => f.Name)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.City,
                f.Province,
                Licence = f.Licences
                    .OrderByDescending(l => l.IssuedAtUtc)
                    .Select(l => new { l.LicenceNumber, l.Status, l.ExpiresAtUtc })
                    .FirstOrDefault(),
                OpenViolations = f.Inspections
                    .SelectMany(i => i.Violations)
                    .Count(v => v.Status != ViolationStatus.Remediated)
            })
            .Select(f => new PublicFacilityListing(
                f.Id,
                f.Name,
                f.City,
                f.Province,
                f.Licence != null ? f.Licence.LicenceNumber : null,
                f.Licence != null ? f.Licence.Status.ToString() : Unlicensed,
                f.Licence != null ? f.Licence.ExpiresAtUtc : null,
                f.OpenViolations))
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicFacilityDetail?> GetAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Facilities
            .AsNoTracking()
            .Where(f => f.Id == facilityId)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.AddressLine1,
                f.AddressLine2,
                f.City,
                f.Province,
                f.PostalCode,
                Licence = f.Licences
                    .OrderByDescending(l => l.IssuedAtUtc)
                    .Select(l => new { l.LicenceNumber, l.Status, l.IssuedAtUtc, l.ExpiresAtUtc })
                    .FirstOrDefault(),
                LicensedCapacity = f.Rooms.Sum(r => r.LicensedCapacity ?? 0),
                Inspections = f.Inspections
                    .OrderByDescending(i => i.InspectionDateUtc)
                    .Select(i => new PublicInspectionSummary(
                        i.InspectionDateUtc,
                        i.Summary,
                        i.Violations.Count,
                        i.Violations.Count(v => v.Severity == ViolationSeverity.Critical)))
                    .ToList()
            })
            .Select(f => new PublicFacilityDetail(
                f.Id,
                f.Name,
                f.AddressLine1,
                f.AddressLine2,
                f.City,
                f.Province,
                f.PostalCode,
                f.Licence != null ? f.Licence.LicenceNumber : null,
                f.Licence != null ? f.Licence.Status.ToString() : Unlicensed,
                f.Licence != null ? f.Licence.IssuedAtUtc : null,
                f.Licence != null ? f.Licence.ExpiresAtUtc : null,
                f.LicensedCapacity,
                f.Inspections))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
