using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChildCareLicensing.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the reporting stored procedures. The aggregation lives in the database so the
    /// API only streams the summarised rows, and so the same procedures can be pointed at
    /// by an SSRS report without duplicating the logic.
    /// </summary>
    public partial class AddComplianceReportingProcedures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ViolationsByCategory);
            migrationBuilder.Sql(FacilityComplianceSummary);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_ViolationsByCategory;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_FacilityComplianceSummary;");
        }

        private const string ViolationsByCategory = """
            CREATE OR ALTER PROCEDURE dbo.usp_ViolationsByCategory
                @LookbackDays INT = 365
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @Cutoff DATETIME2(7) = DATEADD(DAY, -@LookbackDays, SYSUTCDATETIME());

                SELECT
                    v.Category                                                        AS Category,
                    v.Severity                                                        AS Severity,
                    COUNT(*)                                                          AS TotalCount,
                    SUM(CASE WHEN v.Status <> 'Remediated' THEN 1 ELSE 0 END)         AS OpenCount,
                    SUM(CASE WHEN v.Status <> 'Remediated'
                              AND v.RemediationDeadlineUtc < SYSUTCDATETIME()
                             THEN 1 ELSE 0 END)                                       AS OverdueCount
                FROM dbo.Violations AS v
                INNER JOIN dbo.Inspections AS i
                    ON i.Id = v.InspectionId
                WHERE i.InspectionDateUtc >= @Cutoff
                GROUP BY v.Category, v.Severity
                ORDER BY OverdueCount DESC, TotalCount DESC, v.Category;
            END;
            """;

        private const string FacilityComplianceSummary = """
            CREATE OR ALTER PROCEDURE dbo.usp_FacilityComplianceSummary
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

                WITH CurrentLicence AS (
                    SELECT
                        l.FacilityId,
                        l.Status,
                        l.ExpiresAtUtc,
                        ROW_NUMBER() OVER (PARTITION BY l.FacilityId ORDER BY l.IssuedAtUtc DESC) AS RowRank
                    FROM dbo.Licences AS l
                ),
                LastInspection AS (
                    SELECT
                        i.FacilityId,
                        MAX(i.InspectionDateUtc) AS LastInspectionDateUtc
                    FROM dbo.Inspections AS i
                    GROUP BY i.FacilityId
                ),
                ViolationCounts AS (
                    SELECT
                        i.FacilityId,
                        SUM(CASE WHEN v.Status <> 'Remediated' THEN 1 ELSE 0 END) AS OpenViolations,
                        SUM(CASE WHEN v.Status <> 'Remediated'
                                  AND v.RemediationDeadlineUtc < @Now
                                 THEN 1 ELSE 0 END)                               AS OverdueViolations
                    FROM dbo.Violations AS v
                    INNER JOIN dbo.Inspections AS i
                        ON i.Id = v.InspectionId
                    GROUP BY i.FacilityId
                )
                SELECT
                    f.Id                                        AS FacilityId,
                    f.Name                                      AS FacilityName,
                    f.City                                      AS City,
                    ISNULL(cl.Status, 'Unlicensed')             AS LicenceStatus,
                    cl.ExpiresAtUtc                             AS LicenceExpiresAtUtc,
                    DATEDIFF(DAY, @Now, cl.ExpiresAtUtc)        AS DaysUntilExpiry,
                    li.LastInspectionDateUtc                    AS LastInspectionDateUtc,
                    ISNULL(vc.OpenViolations, 0)                AS OpenViolations,
                    ISNULL(vc.OverdueViolations, 0)             AS OverdueViolations,
                    CASE
                        WHEN ISNULL(vc.OverdueViolations, 0) > 0 THEN 'High'
                        WHEN cl.Status IS NULL OR cl.Status <> 'Active' THEN 'High'
                        WHEN DATEDIFF(DAY, @Now, cl.ExpiresAtUtc) <= 60 THEN 'Medium'
                        WHEN li.LastInspectionDateUtc IS NULL
                          OR li.LastInspectionDateUtc < DATEADD(DAY, -365, @Now) THEN 'Medium'
                        WHEN ISNULL(vc.OpenViolations, 0) > 0 THEN 'Medium'
                        ELSE 'Low'
                    END                                         AS RiskLevel
                FROM dbo.Facilities AS f
                LEFT JOIN CurrentLicence AS cl
                    ON cl.FacilityId = f.Id AND cl.RowRank = 1
                LEFT JOIN LastInspection AS li
                    ON li.FacilityId = f.Id
                LEFT JOIN ViolationCounts AS vc
                    ON vc.FacilityId = f.Id
                ORDER BY
                    CASE
                        WHEN ISNULL(vc.OverdueViolations, 0) > 0 THEN 0
                        WHEN cl.Status IS NULL OR cl.Status <> 'Active' THEN 1
                        ELSE 2
                    END,
                    f.Name;
            END;
            """;
    }
}
