using ChildCareLicensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChildCareLicensing.Infrastructure.Persistence.Configurations;

public class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.ToTable("Operators");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.LegalName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.ContactEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(o => o.ContactPhone)
            .HasMaxLength(30);

        builder.HasIndex(o => o.ContactEmail);
    }
}
