using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.DataStore.Json;
using Umea.se.EstateService.DataStore.SqlServer;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.DataStore;

/// <summary>
/// Extension methods for configuring data store persistence services.
/// </summary>
public static class DependencyInjectionDataStore
{
    /// <summary>
    /// Adds the data store persistence implementation to the service collection.
    /// Uses SQL Server when a connection string is configured, otherwise falls back to JSON file persistence.
    /// </summary>
    public static IServiceCollection AddDataStorePersistence(this IServiceCollection services, string? connectionString, DataSyncConfiguration dataSync)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return services.AddSqlServerPersistence(connectionString);
        }
        else
        {
            return services.AddJsonFilePersistence(dataSync.CacheFilePath);
        }
    }

    /// <summary>
    /// Adds the JSON file persistence implementation to the service collection.
    /// </summary>
    private static IServiceCollection AddJsonFilePersistence(this IServiceCollection services, string cacheFilePath)
    {
        services.Configure<JsonFilePersistenceOptions>(options =>
        {
            options.CacheFilePath = cacheFilePath;
        });

        services.AddSingleton<IDataStorePersistence, JsonFilePersistence>();

        return services;
    }

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
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(120); // 2 minute timeout for bulk operations

                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddSingleton<IDataStorePersistence, SqlServerPersistence>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// Should be called during application startup when using SQL Server persistence.
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        IDbContextFactory<EstateDbContext>? dbContextFactory = services.GetService<IDbContextFactory<EstateDbContext>>();
        if (dbContextFactory is null)
        {
            // SQL Server persistence is not configured
            return;
        }

        await using EstateDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
