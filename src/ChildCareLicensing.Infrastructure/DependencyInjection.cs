using ChildCareLicensing.Application.Abstractions;
using ChildCareLicensing.Application.Facilities;
using ChildCareLicensing.Application.Identity;
using ChildCareLicensing.Application.LicenceApplications;
using ChildCareLicensing.Application.PublicRegistry;
using ChildCareLicensing.Application.Reporting;
using ChildCareLicensing.Domain.Entities;
using ChildCareLicensing.Infrastructure.BackgroundServices;
using ChildCareLicensing.Infrastructure.Identity;
using ChildCareLicensing.Infrastructure.Persistence;
using ChildCareLicensing.Infrastructure.Persistence.Reporting;
using ChildCareLicensing.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<ILicenceApplicationReviewRepository, LicenceApplicationReviewRepository>();
        services.AddScoped<IFacilityQueryService, FacilityQueryService>();
        services.AddScoped<IPublicRegistryService, PublicRegistryService>();

        services.AddSingleton(new SqlConnectionProvider(connectionString));
        services.AddScoped<IComplianceReportService, ComplianceReportService>();

        services.AddScoped<LicenceMaintenanceRunner>();

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IUserAccountService, UserAccountService>();

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
