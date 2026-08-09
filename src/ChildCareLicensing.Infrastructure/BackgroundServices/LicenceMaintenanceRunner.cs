using ChildCareLicensing.Domain.Enums;
using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChildCareLicensing.Infrastructure.BackgroundServices;

public sealed record LicenceMaintenanceReport(
    int LicencesExpired,
    int RenewalNoticesIssued,
    int InspectionsOverdue,
    int ViolationsEscalated);

/// <summary>
/// Nightly maintenance for the licence register. Kept separate from the hosted service
/// so the logic can be triggered on demand and tested without a timer.
/// </summary>
public sealed class LicenceMaintenanceRunner(
    ApplicationDbContext dbContext,
    ILogger<LicenceMaintenanceRunner> logger)
{
    private static readonly int[] RenewalNoticeDays = [90, 60, 30];
    private const int AnnualInspectionIntervalDays = 365;

    public async Task<LicenceMaintenanceReport> RunAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var licencesExpired = await ExpireLapsedLicencesAsync(nowUtc, cancellationToken);
        var renewalNotices = await IssueRenewalNoticesAsync(nowUtc, cancellationToken);
        var inspectionsOverdue = await CountFacilitiesOverdueForInspectionAsync(nowUtc, cancellationToken);
        var violationsEscalated = await EscalateOverdueViolationsAsync(nowUtc, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var report = new LicenceMaintenanceReport(
            licencesExpired,
            renewalNotices,
            inspectionsOverdue,
            violationsEscalated);

        logger.LogInformation(
            "Licence maintenance complete. Expired: {Expired}, renewal notices: {Notices}, " +
            "inspections overdue: {Overdue}, violations escalated: {Escalated}.",
            report.LicencesExpired,
            report.RenewalNoticesIssued,
            report.InspectionsOverdue,
            report.ViolationsEscalated);

        return report;
    }

    private async Task<int> ExpireLapsedLicencesAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var lapsed = await dbContext.Licences
            .Where(l => l.Status == LicenceStatus.Active && l.ExpiresAtUtc < nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var licence in lapsed)
        {
            licence.Status = LicenceStatus.Expired;
            licence.UpdatedAtUtc = nowUtc;
        }

        return lapsed.Count;
    }

    private async Task<int> IssueRenewalNoticesAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var horizon = nowUtc.AddDays(RenewalNoticeDays.Max());

        var upcoming = await dbContext.Licences
            .AsNoTracking()
            .Where(l => l.Status == LicenceStatus.Active
                        && l.ExpiresAtUtc >= nowUtc
                        && l.ExpiresAtUtc <= horizon)
            .Select(l => new { l.LicenceNumber, l.ExpiresAtUtc })
            .ToListAsync(cancellationToken);

        var issued = 0;

        foreach (var licence in upcoming)
        {
            var daysRemaining = (int)Math.Ceiling((licence.ExpiresAtUtc - nowUtc).TotalDays);
            if (!RenewalNoticeDays.Contains(daysRemaining))
            {
                continue;
            }

            // A real deployment would queue an email here; logging keeps the demo self-contained.
            logger.LogInformation(
                "Renewal notice: licence {LicenceNumber} expires in {Days} days ({ExpiryDate:yyyy-MM-dd}).",
                licence.LicenceNumber,
                daysRemaining,
                licence.ExpiresAtUtc);

            issued++;
        }

        return issued;
    }

    private async Task<int> CountFacilitiesOverdueForInspectionAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var cutoff = nowUtc.AddDays(-AnnualInspectionIntervalDays);

        var overdue = await dbContext.Facilities
            .AsNoTracking()
            .Where(f => f.Licences.Any(l => l.Status == LicenceStatus.Active))
            .Select(f => new
            {
                f.Name,
                LastInspection = f.Inspections
                    .OrderByDescending(i => i.InspectionDateUtc)
                    .Select(i => (DateTime?)i.InspectionDateUtc)
                    .FirstOrDefault()
            })
            .Where(f => f.LastInspection == null || f.LastInspection < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var facility in overdue)
        {
            logger.LogWarning(
                "Facility {FacilityName} is overdue for its annual inspection (last: {LastInspection}).",
                facility.Name,
                facility.LastInspection?.ToString("yyyy-MM-dd") ?? "never");
        }

        return overdue.Count;
    }

    private async Task<int> EscalateOverdueViolationsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var overdue = await dbContext.Violations
            .Where(v => v.Status == ViolationStatus.Open && v.RemediationDeadlineUtc < nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var violation in overdue)
        {
            violation.Status = ViolationStatus.Escalated;
            violation.UpdatedAtUtc = nowUtc;
        }

        return overdue.Count;
    }
}
