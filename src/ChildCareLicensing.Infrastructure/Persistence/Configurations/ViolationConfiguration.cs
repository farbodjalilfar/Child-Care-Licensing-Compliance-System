using ChildCareLicensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChildCareLicensing.Infrastructure.Persistence.Configurations;

public class ViolationConfiguration : IEntityTypeConfiguration<Violation>
{
    public void Configure(EntityTypeBuilder<Violation> builder)
    {
        builder.ToTable("Violations");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(v => v.Severity)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(v => v.Inspection)
            .WithMany(i => i.Violations)
            .HasForeignKey(v => v.InspectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.Status);
    }
}
