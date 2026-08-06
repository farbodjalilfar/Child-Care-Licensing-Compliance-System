using ChildCareLicensing.Domain.Entities;
using ChildCareLicensing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Operators.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var operatorEntity = new Operator
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            LegalName = "Sunshine Child Care Inc.",
            ContactEmail = "maria@sunshinechildcare.example",
            ContactPhone = "416-555-0100",
            CreatedAtUtc = now
        };

        var facility = new Facility
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            OperatorId = operatorEntity.Id,
            Name = "Sunshine Early Learning Centre",
            AddressLine1 = "100 Queen Street West",
            City = "Toronto",
            Province = "ON",
            PostalCode = "M5H 2N2",
            CreatedAtUtc = now
        };

        var infantRoom = new Room
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            FacilityId = facility.Id,
            Name = "Infant Room A",
            AgeGroup = AgeGroup.Infant,
            FloorAreaSqM = 45,
            ProposedCapacity = 10,
            CreatedAtUtc = now
        };

        var toddlerRoom = new Room
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            FacilityId = facility.Id,
            Name = "Toddler Room B",
            AgeGroup = AgeGroup.Toddler,
            FloorAreaSqM = 55,
            ProposedCapacity = 15,
            CreatedAtUtc = now
        };

        var application = new LicenceApplication
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            FacilityId = facility.Id,
            Status = ApplicationStatus.Draft,
            CreatedAtUtc = now
        };

        context.Operators.Add(operatorEntity);
        context.Facilities.Add(facility);
        context.Rooms.AddRange(infantRoom, toddlerRoom);
        context.LicenceApplications.Add(application);

        await context.SaveChangesAsync(cancellationToken);
    }
}
