using ChildCareLicensing.Application.LicenceApplications;
using Microsoft.Extensions.DependencyInjection;

namespace ChildCareLicensing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILicenceApplicationService, LicenceApplicationService>();
        return services;
    }
}
