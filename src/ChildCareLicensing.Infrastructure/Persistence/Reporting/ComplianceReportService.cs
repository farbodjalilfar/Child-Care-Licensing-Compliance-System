using System.Data;
using ChildCareLicensing.Application.Reporting;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ChildCareLicensing.Infrastructure.Persistence.Reporting;

/// <summary>
/// Uses Dapper rather than EF Core because these queries are aggregate reports served by
/// stored procedures. Keeping the aggregation in the database avoids pulling rows into
/// memory and keeps the logic portable to SSRS.
/// </summary>
public sealed class ComplianceReportService(SqlConnectionProvider connectionProvider) : IComplianceReportService
{
    public async Task<IReadOnlyList<ViolationsByCategoryRow>> GetViolationsByCategoryAsync(
        int lookbackDays,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionProvider.Create();

        var command = new CommandDefinition(
            "dbo.usp_ViolationsByCategory",
            new { LookbackDays = lookbackDays },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<ViolationsByCategoryRow>(command);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<FacilityComplianceRow>> GetFacilityComplianceAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionProvider.Create();

        var command = new CommandDefinition(
            "dbo.usp_FacilityComplianceSummary",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<FacilityComplianceRow>(command);
        return rows.ToList();
    }
}

/// <summary>
/// Hands out raw ADO.NET connections for the Dapper-based reporting queries.
/// </summary>
public sealed class SqlConnectionProvider(string connectionString)
{
    public SqlConnection Create() => new(connectionString);
}
