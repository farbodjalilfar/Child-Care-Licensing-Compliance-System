using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ChildCareLicensing.IntegrationTests;

/// <summary>
/// Boots the real API pipeline against a private SQLite database, so the tests exercise
/// routing, model binding, dependency injection and EF Core query translation without
/// needing a SQL Server instance in CI.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // The real connection string is never used: the SQLite registration below replaces the
        // DbContext. It only needs to be present so composition of the data layer succeeds.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=(local);Database=Test;");

        builder.ConfigureServices(services =>
        {
            // EF Core applies every registered options configuration, so the SQL Server one
            // has to go before SQLite is added or both providers end up on the same context.
            var providerRegistrations = services
                .Where(d => d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true)
                .ToList();

            foreach (var registration in providerRegistrations)
            {
                services.Remove(registration);
            }

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ApplicationDbContext>();

            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureCreatedAsync();
        await DevelopmentDataSeeder.SeedAsync(context);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
