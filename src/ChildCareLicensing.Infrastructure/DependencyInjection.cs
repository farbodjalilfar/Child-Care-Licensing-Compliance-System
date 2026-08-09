using ChildCareLicensing.Application.Abstractions;
using ChildCareLicensing.Application.Facilities;
using ChildCareLicensing.Application.PublicRegistry;
using ChildCareLicensing.Application.Reporting;
using ChildCareLicensing.Infrastructure.BackgroundServices;
using ChildCareLicensing.Infrastructure.Persistence;
using ChildCareLicensing.Infrastructure.Persistence.Reporting;
using ChildCareLicensing.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChildCareLicensing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ILicenceApplicationRepository, LicenceApplicationRepository>();
        services.AddScoped<IFacilityQueryService, FacilityQueryService>();
        services.AddScoped<IPublicRegistryService, PublicRegistryService>();

        services.AddSingleton(new SqlConnectionProvider(connectionString));
        services.AddScoped<IComplianceReportService, ComplianceReportService>();

        services.AddScoped<LicenceMaintenanceRunner>();

        return services;
    }

    /// <summary>
    /// Registered separately so tests can compose the data layer without a timer running.
    /// </summary>
    public static IServiceCollection AddLicenceMaintenanceWorker(this IServiceCollection services)
    {
        services.AddHostedService<LicenceMaintenanceService>();
        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        var password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            return connectionString;
        }

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            Password = password
        };

        return builder.ConnectionString;
    }
}
