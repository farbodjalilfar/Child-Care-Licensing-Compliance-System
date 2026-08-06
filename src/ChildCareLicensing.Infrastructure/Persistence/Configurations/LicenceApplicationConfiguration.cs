using ChildCareLicensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChildCareLicensing.Infrastructure.Persistence.Configurations;

public class LicenceApplicationConfiguration : IEntityTypeConfiguration<LicenceApplication>
{
    public void Configure(EntityTypeBuilder<LicenceApplication> builder)
    {
        builder.ToTable("LicenceApplications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.ReviewerNotes)
            .HasMaxLength(2000);

        builder.HasOne(a => a.Facility)
            .WithMany(f => f.Applications)
            .HasForeignKey(a => a.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.Status);
    }
}
