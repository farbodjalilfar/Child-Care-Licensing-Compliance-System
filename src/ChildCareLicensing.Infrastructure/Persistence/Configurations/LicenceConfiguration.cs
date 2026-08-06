using ChildCareLicensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChildCareLicensing.Infrastructure.Persistence.Configurations;

public class LicenceConfiguration : IEntityTypeConfiguration<Licence>
{
    public void Configure(EntityTypeBuilder<Licence> builder)
    {
        builder.ToTable("Licences");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.LicenceNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(l => l.Facility)
            .WithMany(f => f.Licences)
            .HasForeignKey(l => l.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Application)
            .WithOne(a => a.Licence)
            .HasForeignKey<Licence>(l => l.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.LicenceNumber)
            .IsUnique();

        builder.HasIndex(l => l.Status);
    }
}
