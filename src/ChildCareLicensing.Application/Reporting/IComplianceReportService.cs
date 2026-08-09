namespace ChildCareLicensing.Application.Reporting;

/// <summary>
/// Compliance reporting is served by SQL Server stored procedures rather than the ORM,
/// so the aggregation runs in the database and the logic stays portable to SSRS.
/// </summary>
public interface IComplianceReportService
{
    Task<IReadOnlyList<ViolationsByCategoryRow>> GetViolationsByCategoryAsync(
        int lookbackDays,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FacilityComplianceRow>> GetFacilityComplianceAsync(
        CancellationToken cancellationToken = default);
}

public sealed record ViolationsByCategoryRow(
    string Category,
    string Severity,
    int TotalCount,
    int OpenCount,
    int OverdueCount);

public sealed record FacilityComplianceRow(
    Guid FacilityId,
    string FacilityName,
    string City,
    string LicenceStatus,
    DateTime? LicenceExpiresAtUtc,
    int? DaysUntilExpiry,
    DateTime? LastInspectionDateUtc,
    int OpenViolations,
    int OverdueViolations,
    string RiskLevel);
