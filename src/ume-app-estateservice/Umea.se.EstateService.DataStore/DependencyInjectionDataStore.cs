using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.DataStore.Sqlite;
using Umea.se.EstateService.DataStore.SqlServer;
using Umea.se.EstateService.Shared.Data;

namespace Umea.se.EstateService.DataStore;

/// <summary>
/// Extension methods for configuring data store persistence services.
/// </summary>
public static class DependencyInjectionDataStore
{
    /// <summary>
    /// Adds the data store persistence implementation to the service collection.
    /// Auto-detects the database provider from the connection string format:
    /// - SQL Server when the string contains "Server=", "Initial Catalog=", or "Integrated Security="
    /// - SQLite otherwise (e.g. "Data Source=file.db")
    /// </summary>
    public static IServiceCollection AddDataStorePersistence(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string for 'EstateService' is required. " +
                "Use SQLite (e.g. 'Data Source=estateservice.db') for local development " +
                "or SQL Server for production.");
        }

        if (IsSqlServer(connectionString))
        {
            return services.AddSqlServerPersistence(connectionString);
        }

        return services.AddSqlitePersistence(connectionString);
    }

    /// <summary>
    /// SQL Server strings contain "Server=" or "Initial Catalog=" or "Integrated Security=".
    /// SQLite strings are simpler: "Data Source=file.db" or "DataSource=:memory:".
    /// </summary>
    private static bool IsSqlServer(string connectionString) =>
        connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("Integrated Security=", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Adds the SQL Server persistence implementation to the service collection.
    /// </summary>
    private static IServiceCollection AddSqlServerPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Register DbContext factory for efficient context creation
        services.AddDbContextFactory<EstateDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: [-2]); // -2 = SQL timeout (CommandTimeout expired)

                sqlOptions.CommandTimeout(120); // 2 minute timeout for bulk operations

                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddSingleton<IDataStorePersistence, SqlServerPersistence>();
        services.AddRepositories();

        return services;
    }

    /// <summary>
    /// Adds the SQLite persistence implementation to the service collection.
    /// </summary>
    private static IServiceCollection AddSqlitePersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContextFactory<EstateDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddSingleton<IDataStorePersistence, SqlitePersistence>();
        services.AddRepositories();

        return services;
    }

    /// <summary>
    /// Registers repository services. Separated from provider setup so tests can call this
    /// after configuring their own DbContext without duplicating registrations.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();

        return services;
    }

    /// <summary>
    /// Ensures the database exists and schema is up to date.
    /// SQLite (both in-memory and file-based) uses EnsureCreatedAsync because
    /// EF migrations are scaffolded for SQL Server and contain incompatible DDL.
    /// SQL Server migrations are handled by the deployment pipeline.
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        IDbContextFactory<EstateDbContext> dbContextFactory = services.GetRequiredService<IDbContextFactory<EstateDbContext>>();

        await using EstateDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
        // SQL Server migrations are handled by the deployment pipeline
    }
}
