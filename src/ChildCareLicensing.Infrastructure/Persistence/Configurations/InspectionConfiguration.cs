using ChildCareLicensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChildCareLicensing.Infrastructure.Persistence.Configurations;

public class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        builder.ToTable("Inspections");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InspectorName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Summary)
            .HasMaxLength(2000);

        builder.HasOne(i => i.Facility)
            .WithMany(f => f.Inspections)
            .HasForeignKey(i => i.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.InspectionDateUtc);
    }
}
