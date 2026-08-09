using ChildCareLicensing.Application.LicenceApplications;
using ChildCareLicensing.Domain.Entities;
using ChildCareLicensing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Persistence.Repositories;

public sealed class LicenceApplicationReviewRepository(ApplicationDbContext dbContext)
    : ILicenceApplicationReviewRepository
{
    private const string LicencePrefix = "ON-CCL-";
    private const int LicenceNumberDigits = 6;

    public async Task<IReadOnlyList<ReviewQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.LicenceApplications
            .AsNoTracking()
            .Where(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview)
            .OrderBy(a => a.SubmittedAtUtc)
            .Select(a => new ReviewQueueItem(
                a.Id,
                a.FacilityId,
                a.Facility.Name,
                a.Facility.City,
                a.Facility.Operator.LegalName,
                a.Status.ToString(),
                a.SubmittedAtUtc,
                a.Facility.Rooms.Count,
                a.Facility.Rooms.Sum(r => r.LicensedCapacity ?? r.ProposedCapacity)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationHistoryEntry>> GetHistoryAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ApplicationStatusHistory
            .AsNoTracking()
            .Where(h => h.ApplicationId == applicationId)
            .OrderByDescending(h => h.ChangedAtUtc)
            .Select(h => new ApplicationHistoryEntry(
                h.FromStatus == null ? null : h.FromStatus.ToString(),
                h.ToStatus.ToString(),
                h.ChangedAtUtc,
                h.ChangedBy,
                h.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicationStatus?> GetStatusAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.LicenceApplications
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => (ApplicationStatus?)a.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RecordTransitionAsync(
        Guid applicationId,
        ApplicationStatus from,
        ApplicationStatus to,
        string changedBy,
        string? notes,
        DateTime changedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LicenceApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        application.Status = to;
        application.UpdatedAtUtc = changedAtUtc;

        if (to is ApplicationStatus.AdditionalInfoRequired or ApplicationStatus.Rejected)
        {
            application.ReviewerNotes = notes;
        }

        if (to is ApplicationStatus.Approved or ApplicationStatus.Rejected)
        {
            application.ReviewedAtUtc = changedAtUtc;
        }

        dbContext.ApplicationStatusHistory.Add(NewHistoryEntry(
            applicationId, from, to, changedBy, notes, changedAtUtc));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> ApproveAndIssueLicenceAsync(
        Guid applicationId,
        string reviewer,
        string? notes,
        DateTime decidedAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        // Approving and issuing must not be separable: an approved application without a
        // licence, or a licence without an approval, would both be wrong.
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var application = await dbContext.LicenceApplications
                .Include(a => a.Facility)
                .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
                ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

            var previousStatus = application.Status;

            application.Status = ApplicationStatus.Approved;
            application.ReviewedAtUtc = decidedAtUtc;
            application.ReviewerNotes = notes;
            application.UpdatedAtUtc = decidedAtUtc;

            var licenceNumber = await NextLicenceNumberAsync(cancellationToken);

            dbContext.Licences.Add(new Licence
            {
                Id = Guid.NewGuid(),
                FacilityId = application.FacilityId,
                ApplicationId = application.Id,
                LicenceNumber = licenceNumber,
                IssuedAtUtc = decidedAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                Status = LicenceStatus.Active,
                CreatedAtUtc = decidedAtUtc
            });

            dbContext.ApplicationStatusHistory.Add(NewHistoryEntry(
                applicationId,
                previousStatus,
                ApplicationStatus.Approved,
                reviewer,
                notes is null
                    ? $"Approved. Licence {licenceNumber} issued."
                    : $"Approved. Licence {licenceNumber} issued. {notes}",
                decidedAtUtc));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return licenceNumber;
        });
    }

    private async Task<string> NextLicenceNumberAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.Licences
            .AsNoTracking()
            .Select(l => l.LicenceNumber)
            .ToListAsync(cancellationToken);

        var highest = existing
            .Where(n => n.StartsWith(LicencePrefix, StringComparison.Ordinal))
            .Select(n => int.TryParse(n[LicencePrefix.Length..], out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max();

        return LicencePrefix + (highest + 1).ToString(new string('0', LicenceNumberDigits));
    }

    private static ApplicationStatusHistory NewHistoryEntry(
        Guid applicationId,
        ApplicationStatus from,
        ApplicationStatus to,
        string changedBy,
        string? notes,
        DateTime changedAtUtc) => new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            FromStatus = from,
            ToStatus = to,
            ChangedAtUtc = changedAtUtc,
            ChangedBy = changedBy,
            Notes = notes,
            CreatedAtUtc = changedAtUtc
        };
}
