using ChildCareLicensing.Domain.Entities;
using ChildCareLicensing.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Persistence;

/// <summary>
/// Seeds a demo register. Dates are relative to the current date so the public registry,
/// compliance report and expiry worker always have meaningful data to show.
/// </summary>
public static class DevelopmentDataSeeder
{
    public const string DemoPassword = "Demo!2345";

    private static readonly Guid SunshineOperatorId = Guid.Parse("0a000001-0000-4000-8000-000000000001");
    private static readonly Guid MapleGroveOperatorId = Guid.Parse("0a000002-0000-4000-8000-000000000002");
    private static readonly Guid RiversideOperatorId = Guid.Parse("0a000003-0000-4000-8000-000000000003");

    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Operators.AnyAsync(cancellationToken))
        {
            // A database seeded before sign-in existed still needs the demo accounts.
            if (await SeedDemoUsersAsync(context, cancellationToken))
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        await SeedDemoUsersAsync(context, cancellationToken);

        var now = DateTime.UtcNow;
        var today = now.Date;

        var sunshine = new Operator
        {
            Id = SunshineOperatorId,
            LegalName = "Sunshine Child Care Inc.",
            ContactEmail = "maria@sunshinechildcare.example",
            ContactPhone = "416-555-0100",
            CreatedAtUtc = now
        };

        var mapleGrove = new Operator
        {
            Id = MapleGroveOperatorId,
            LegalName = "Maple Grove Early Years Ltd.",
            ContactEmail = "admin@maplegrove.example",
            ContactPhone = "613-555-0142",
            CreatedAtUtc = now
        };

        var riverside = new Operator
        {
            Id = RiversideOperatorId,
            LegalName = "Riverside Community Services",
            ContactEmail = "office@riversidecs.example",
            ContactPhone = "905-555-0188",
            CreatedAtUtc = now
        };

        context.Operators.AddRange(sunshine, mapleGrove, riverside);

        // Facility 1: still drafting its first application. This is the walkthrough facility
        // referenced by the README, so its identifiers are fixed.
        var sunshineFacility = new Facility
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            OperatorId = sunshine.Id,
            Name = "Sunshine Early Learning Centre",
            AddressLine1 = "100 Queen Street West",
            City = "Toronto",
            Province = "ON",
            PostalCode = "M5H 2N2",
            CreatedAtUtc = now
        };

        context.Facilities.Add(sunshineFacility);

        context.Rooms.AddRange(
            new Room
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FacilityId = sunshineFacility.Id,
                Name = "Infant Room A",
                AgeGroup = AgeGroup.Infant,
                FloorAreaSqM = 45,
                ProposedCapacity = 10,
                CreatedAtUtc = now
            },
            new Room
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                FacilityId = sunshineFacility.Id,
                Name = "Toddler Room B",
                AgeGroup = AgeGroup.Toddler,
                FloorAreaSqM = 55,
                ProposedCapacity = 15,
                CreatedAtUtc = now
            });

        context.LicenceApplications.Add(new LicenceApplication
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            FacilityId = sunshineFacility.Id,
            Status = ApplicationStatus.Draft,
            CreatedAtUtc = now
        });

        // Facility 2: licensed, renewal window opening soon, one outstanding minor violation.
        SeedLicensedFacility(
            context,
            now,
            operatorId: mapleGrove.Id,
            facilityId: Guid.Parse("0fac0002-0000-4000-8000-000000000002"),
            name: "Maple Grove Children's Centre",
            addressLine1: "480 Bank Street",
            city: "Ottawa",
            postalCode: "K2P 1Z4",
            licenceNumber: "ON-CCL-004821",
            issuedAtUtc: today.AddDays(-320),
            // Lands exactly on a renewal-notice threshold so the maintenance worker has
            // something to report on its first run.
            expiresAtUtc: today.AddDays(60),
            rooms:
            [
                ("Toddler Room 1", AgeGroup.Toddler, 52m, 14),
                ("Preschool Room 2", AgeGroup.Preschool, 62m, 16)
            ],
            inspections:
            [
                new SeedInspection(
                    today.AddDays(-210),
                    "Priya Raman",
                    "Annual compliance inspection. Two minor findings issued.",
                    [
                        new SeedViolation(
                            "Record Keeping",
                            "Daily attendance register was missing entries for three days.",
                            ViolationSeverity.Minor,
                            ViolationStatus.Remediated,
                            DeadlineOffsetDays: -180),
                        new SeedViolation(
                            "Playground Safety",
                            "Protective surfacing under the climber was below the required depth.",
                            ViolationSeverity.Moderate,
                            ViolationStatus.Open,
                            DeadlineOffsetDays: 21)
                    ]),
                new SeedInspection(
                    today.AddDays(-60),
                    "Priya Raman",
                    "Follow-up visit. Attendance records corrected.",
                    [])
            ]);

        // Facility 3: licensed, but sitting on an overdue critical finding.
        SeedLicensedFacility(
            context,
            now,
            operatorId: riverside.Id,
            facilityId: Guid.Parse("0fac0003-0000-4000-8000-000000000003"),
            name: "Riverside Kids Club",
            addressLine1: "18 King Street East",
            city: "Hamilton",
            postalCode: "L8N 1A1",
            licenceNumber: "ON-CCL-005190",
            issuedAtUtc: today.AddDays(-95),
            expiresAtUtc: today.AddDays(270),
            rooms:
            [
                ("School Age Room", AgeGroup.SchoolAge, 88m, 26),
                ("Preschool Room", AgeGroup.Preschool, 50m, 16)
            ],
            inspections:
            [
                new SeedInspection(
                    today.AddDays(-40),
                    "Daniel Okafor",
                    "Complaint-driven inspection. One critical finding issued.",
                    [
                        new SeedViolation(
                            "Staff Ratios",
                            "School age room operated above the permitted staff-to-child ratio during pickup.",
                            ViolationSeverity.Critical,
                            ViolationStatus.Open,
                            DeadlineOffsetDays: -12),
                        new SeedViolation(
                            "Medication Handling",
                            "Medication log was not countersigned by a second staff member.",
                            ViolationSeverity.Moderate,
                            ViolationStatus.Remediated,
                            DeadlineOffsetDays: -20)
                    ])
            ]);

        // Facility 4: licence already lapsed and no inspection in over a year.
        SeedLicensedFacility(
            context,
            now,
            operatorId: riverside.Id,
            facilityId: Guid.Parse("0fac0004-0000-4000-8000-000000000004"),
            name: "Lakeshore Montessori",
            addressLine1: "2201 Lake Shore Boulevard West",
            city: "Toronto",
            postalCode: "M8V 1A1",
            licenceNumber: "ON-CCL-003377",
            issuedAtUtc: today.AddDays(-380),
            expiresAtUtc: today.AddDays(-15),
            licenceStatus: LicenceStatus.Expired,
            rooms:
            [
                ("Infant Room", AgeGroup.Infant, 40m, 12),
                ("Toddler Room", AgeGroup.Toddler, 48m, 15)
            ],
            inspections:
            [
                new SeedInspection(
                    today.AddDays(-400),
                    "Daniel Okafor",
                    "Annual compliance inspection. No findings.",
                    [])
            ]);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void SeedLicensedFacility(
        ApplicationDbContext context,
        DateTime now,
        Guid operatorId,
        Guid facilityId,
        string name,
        string addressLine1,
        string city,
        string postalCode,
        string licenceNumber,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc,
        IReadOnlyList<(string Name, AgeGroup AgeGroup, decimal FloorAreaSqM, int Capacity)> rooms,
        IReadOnlyList<SeedInspection> inspections,
        LicenceStatus licenceStatus = LicenceStatus.Active)
    {
        var facility = new Facility
        {
            Id = facilityId,
            OperatorId = operatorId,
            Name = name,
            AddressLine1 = addressLine1,
            City = city,
            Province = "ON",
            PostalCode = postalCode,
            CreatedAtUtc = now
        };

        context.Facilities.Add(facility);

        foreach (var (roomName, ageGroup, floorArea, capacity) in rooms)
        {
            context.Rooms.Add(new Room
            {
                Id = Guid.NewGuid(),
                FacilityId = facility.Id,
                Name = roomName,
                AgeGroup = ageGroup,
                FloorAreaSqM = floorArea,
                ProposedCapacity = capacity,
                LicensedCapacity = capacity,
                CreatedAtUtc = now
            });
        }

        var application = new LicenceApplication
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            Status = ApplicationStatus.Approved,
            SubmittedAtUtc = issuedAtUtc.AddDays(-30),
            ReviewedAtUtc = issuedAtUtc,
            ReviewerNotes = "Capacity validated against age-group ratio and floor-area rules.",
            CreatedAtUtc = issuedAtUtc.AddDays(-45)
        };

        context.LicenceApplications.Add(application);

        AddStatusHistory(context, application.Id, null, ApplicationStatus.Draft,
            application.CreatedAtUtc, "system", "Application created.");
        AddStatusHistory(context, application.Id, ApplicationStatus.Draft, ApplicationStatus.Submitted,
            application.SubmittedAtUtc!.Value, "operator", "Submitted after capacity validation passed.");
        AddStatusHistory(context, application.Id, ApplicationStatus.Submitted, ApplicationStatus.UnderReview,
            application.SubmittedAtUtc!.Value.AddDays(2), "j.tremblay@ontario.example", "Review started.");
        AddStatusHistory(context, application.Id, ApplicationStatus.UnderReview, ApplicationStatus.Approved,
            issuedAtUtc, "j.tremblay@ontario.example", $"Approved. Licence {licenceNumber} issued.");

        context.Licences.Add(new Licence
        {
            Id = Guid.NewGuid(),
            FacilityId = facility.Id,
            ApplicationId = application.Id,
            LicenceNumber = licenceNumber,
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            Status = licenceStatus,
            CreatedAtUtc = issuedAtUtc
        });

        foreach (var seedInspection in inspections)
        {
            var inspection = new Inspection
            {
                Id = Guid.NewGuid(),
                FacilityId = facility.Id,
                InspectionDateUtc = seedInspection.DateUtc,
                InspectorName = seedInspection.InspectorName,
                Summary = seedInspection.Summary,
                CreatedAtUtc = seedInspection.DateUtc
            };

            context.Inspections.Add(inspection);

            foreach (var seedViolation in seedInspection.Violations)
            {
                context.Violations.Add(new Violation
                {
                    Id = Guid.NewGuid(),
                    InspectionId = inspection.Id,
                    Category = seedViolation.Category,
                    Description = seedViolation.Description,
                    Severity = seedViolation.Severity,
                    Status = seedViolation.Status,
                    RemediationDeadlineUtc = now.Date.AddDays(seedViolation.DeadlineOffsetDays),
                    RemediatedAtUtc = seedViolation.Status == ViolationStatus.Remediated
                        ? now.Date.AddDays(seedViolation.DeadlineOffsetDays - 2)
                        : null,
                    CreatedAtUtc = seedInspection.DateUtc
                });
            }
        }
    }


    /// <summary>
    /// Demo accounts. The passwords are deliberately published in the README: this is a
    /// public sample application, and a reviewer needs to be able to sign in and try it.
    /// </summary>
    private static async Task<bool> SeedDemoUsersAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var hasher = new PasswordHasher<User>();

        void AddUser(string email, string displayName, UserRole role, Guid? operatorId)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = displayName,
                Role = role,
                OperatorId = operatorId,
                PasswordHash = string.Empty,
                CreatedAtUtc = now
            };

            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            context.Users.Add(user);
        }

        AddUser("maria@sunshinechildcare.example", "Maria Santos", UserRole.Operator, SunshineOperatorId);
        AddUser("admin@maplegrove.example", "Alex Chen", UserRole.Operator, MapleGroveOperatorId);
        AddUser("j.tremblay@ontario.example", "Julie Tremblay", UserRole.Reviewer, null);
        AddUser("p.raman@ontario.example", "Priya Raman", UserRole.Inspector, null);

        return true;
    }

    private static void AddStatusHistory(
        ApplicationDbContext context,
        Guid applicationId,
        ApplicationStatus? from,
        ApplicationStatus to,
        DateTime changedAtUtc,
        string changedBy,
        string notes)
    {
        context.ApplicationStatusHistory.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            FromStatus = from,
            ToStatus = to,
            ChangedAtUtc = changedAtUtc,
            ChangedBy = changedBy,
            Notes = notes,
            CreatedAtUtc = changedAtUtc
        });
    }

    private sealed record SeedInspection(
        DateTime DateUtc,
        string InspectorName,
        string Summary,
        IReadOnlyList<SeedViolation> Violations);

    private sealed record SeedViolation(
        string Category,
        string Description,
        ViolationSeverity Severity,
        ViolationStatus Status,
        int DeadlineOffsetDays);
}
