using ChildCareLicensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChildCareLicensing.Infrastructure.Persistence.Configurations;

public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("Facilities");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.AddressLine1)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.AddressLine2)
            .HasMaxLength(200);

        builder.Property(f => f.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.Province)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.PostalCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(f => f.Operator)
            .WithMany(o => o.Facilities)
            .HasForeignKey(f => f.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.City, f.Name });
    }
}
