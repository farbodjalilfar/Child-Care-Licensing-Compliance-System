using ChildCareLicensing.Domain.Entities;
using ChildCareLicensing.Domain.Enums;
using ChildCareLicensing.Infrastructure.BackgroundServices;
using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChildCareLicensing.IntegrationTests;

public class LicenceMaintenanceRunnerTests : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private SqliteConnection connection = null!;
    private ApplicationDbContext context = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task RunAsync_ExpiresLapsedLicencesAndEscalatesOverdueViolations()
    {
        var facility = SeedFacility();

        context.Licences.AddRange(
            NewLicence(facility.Id, "ON-CCL-000001", Now.AddDays(-400), Now.AddDays(-1)),
            NewLicence(facility.Id, "ON-CCL-000002", Now.AddDays(-300), Now.AddDays(30)));

        var inspection = new Inspection
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            InspectionDateUtc = Now.AddDays(-30),
            InspectorName = "Inspector",
            CreatedAtUtc = Now.AddDays(-30)
        };

        context.Inspections.Add(inspection);

        context.Violations.AddRange(
            NewViolation(inspection.Id, ViolationStatus.Open, Now.AddDays(-5)),
            NewViolation(inspection.Id, ViolationStatus.Open, Now.AddDays(10)));

        await context.SaveChangesAsync();

        var runner = new LicenceMaintenanceRunner(context, NullLogger<LicenceMaintenanceRunner>.Instance);
        var report = await runner.RunAsync(Now);

        Assert.Equal(1, report.LicencesExpired);
        Assert.Equal(1, report.RenewalNoticesIssued);
        Assert.Equal(1, report.ViolationsEscalated);

        Assert.Equal(
            1,
            await context.Licences.CountAsync(l => l.Status == LicenceStatus.Expired));
        Assert.Equal(
            1,
            await context.Violations.CountAsync(v => v.Status == ViolationStatus.Escalated));
    }

    [Fact]
    public async Task RunAsync_FlagsFacilitiesOverdueForInspection()
    {
        var facility = SeedFacility();

        context.Licences.Add(NewLicence(facility.Id, "ON-CCL-000003", Now.AddDays(-200), Now.AddDays(200)));
        context.Inspections.Add(new Inspection
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            InspectionDateUtc = Now.AddDays(-400),
            InspectorName = "Inspector",
            CreatedAtUtc = Now.AddDays(-400)
        });

        await context.SaveChangesAsync();

        var runner = new LicenceMaintenanceRunner(context, NullLogger<LicenceMaintenanceRunner>.Instance);
        var report = await runner.RunAsync(Now);

        Assert.Equal(1, report.InspectionsOverdue);
    }

    private Facility SeedFacility()
    {
        var operatorEntity = new Operator
        {
            Id = Guid.NewGuid(),
            LegalName = "Test Operator",
            ContactEmail = "test@example.com",
            CreatedAtUtc = Now
        };

        var facility = new Facility
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorEntity.Id,
            Name = "Test Facility",
            AddressLine1 = "1 Test Street",
            City = "Toronto",
            Province = "ON",
            PostalCode = "M1M 1M1",
            CreatedAtUtc = Now
        };

        context.Operators.Add(operatorEntity);
        context.Facilities.Add(facility);

        return facility;
    }

    private static Licence NewLicence(Guid facilityId, string number, DateTime issued, DateTime expires)
    {
        var application = new LicenceApplication
        {
            Id = Guid.NewGuid(),
            FacilityId = facilityId,
            Status = ApplicationStatus.Approved,
            CreatedAtUtc = issued
        };

        return new Licence
        {
            Id = Guid.NewGuid(),
            FacilityId = facilityId,
            ApplicationId = application.Id,
            Application = application,
            LicenceNumber = number,
            IssuedAtUtc = issued,
            ExpiresAtUtc = expires,
            Status = LicenceStatus.Active,
            CreatedAtUtc = issued
        };
    }

    private static Violation NewViolation(Guid inspectionId, ViolationStatus status, DateTime deadline) => new()
    {
        Id = Guid.NewGuid(),
        InspectionId = inspectionId,
        Category = "Staff Ratios",
        Description = "Test finding.",
        Severity = ViolationSeverity.Moderate,
        Status = status,
        RemediationDeadlineUtc = deadline,
        CreatedAtUtc = deadline.AddDays(-10)
    };
}
