using ChildCareLicensing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Operator> Operators => Set<Operator>();

    public DbSet<Facility> Facilities => Set<Facility>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<LicenceApplication> LicenceApplications => Set<LicenceApplication>();

    public DbSet<Licence> Licences => Set<Licence>();

    public DbSet<Inspection> Inspections => Set<Inspection>();

    public DbSet<Violation> Violations => Set<Violation>();

    public DbSet<ApplicationStatusHistory> ApplicationStatusHistory => Set<ApplicationStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
