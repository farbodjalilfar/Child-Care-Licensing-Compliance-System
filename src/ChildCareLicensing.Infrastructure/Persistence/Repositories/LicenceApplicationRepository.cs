using ChildCareLicensing.Application.Abstractions;
using ChildCareLicensing.Domain.Entities;
using ChildCareLicensing.Domain.Enums;
using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Persistence.Repositories;

public sealed class LicenceApplicationRepository(ApplicationDbContext dbContext) : ILicenceApplicationRepository
{
    public Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken = default)
        => dbContext.Facilities.AnyAsync(f => f.Id == facilityId, cancellationToken);

    public async Task<Guid?> GetDraftApplicationIdForFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LicenceApplications
            .AsNoTracking()
            .Where(a => a.FacilityId == facilityId && a.Status == ApplicationStatus.Draft)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<LicenceApplicationDetails?> GetApplicationDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LicenceApplications
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new LicenceApplicationDetails(
                a.Id,
                a.FacilityId,
                a.Facility.Name,
                a.Status.ToString(),
                a.SubmittedAtUtc,
                a.Facility.Rooms
                    .OrderBy(r => r.Name)
                    .Select(r => new LicenceApplicationRoomDetails(
                        r.Id,
                        r.Name,
                        r.AgeGroup.ToString(),
                        r.FloorAreaSqM,
                        r.ProposedCapacity,
                        r.LicensedCapacity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateDraftApplicationAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        var application = new LicenceApplication
        {
            Id = Guid.NewGuid(),
            FacilityId = facilityId,
            Status = ApplicationStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.LicenceApplications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken);
        return application.Id;
    }

    public async Task SubmitApplicationAsync(
        Guid applicationId,
        DateTime submittedAtUtc,
        IReadOnlyDictionary<Guid, int> licensedCapacitiesByRoomId,
        CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LicenceApplications
            .Include(a => a.Facility)
            .ThenInclude(f => f.Rooms)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        foreach (var room in application.Facility.Rooms)
        {
            if (licensedCapacitiesByRoomId.TryGetValue(room.Id, out var licensedCapacity))
            {
                room.LicensedCapacity = licensedCapacity;
                room.UpdatedAtUtc = submittedAtUtc;
            }
        }

        application.Status = ApplicationStatus.Submitted;
        application.SubmittedAtUtc = submittedAtUtc;
        application.UpdatedAtUtc = submittedAtUtc;

        dbContext.ApplicationStatusHistory.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            FromStatus = ApplicationStatus.Draft,
            ToStatus = ApplicationStatus.Submitted,
            ChangedAtUtc = submittedAtUtc,
            ChangedBy = "operator",
            Notes = "Application submitted after capacity validation.",
            CreatedAtUtc = submittedAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
