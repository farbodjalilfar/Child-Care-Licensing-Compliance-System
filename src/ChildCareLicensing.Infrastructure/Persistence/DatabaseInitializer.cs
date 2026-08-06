using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChildCareLicensing.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public static async Task InitializeDevelopmentDatabaseAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        for (var attempt = 1; attempt <= 20; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Connecting to SQL Server (attempt {Attempt}/20)...",
                    attempt);

                await db.Database.MigrateAsync(cancellationToken);
                await DevelopmentDataSeeder.SeedAsync(db, cancellationToken);

                logger.LogInformation("Database is ready.");
                return;
            }
            catch (Exception ex) when (attempt < 20 && IsTransientStartupFailure(ex))
            {
                logger.LogWarning(
                    ex,
                    "Database not ready yet. Retrying in {DelaySeconds}s...",
                    RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private static bool IsTransientStartupFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException ||
                current is IOException ||
                (current is Microsoft.Data.SqlClient.SqlException sql &&
                 (sql.Number is -2 or 233 or 4060 or 10061 or 10054 or 10060)))
            {
                return true;
            }

            if (current.Message.Contains("pre-login handshake", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Connection reset by peer", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
