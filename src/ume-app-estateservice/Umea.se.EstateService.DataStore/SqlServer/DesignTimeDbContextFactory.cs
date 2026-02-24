using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umea.se.EstateService.DataStore.SqlServer;

/// <summary>
/// Design-time factory for creating EstateDbContext instances.
/// Used by EF Core tools for migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EstateDbContext>
{
    public EstateDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<EstateDbContext> optionsBuilder = new();

        string connectionString = Environment.GetEnvironmentVariable("ESTATE_DB_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=EstateService_Dev;Trusted_Connection=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new EstateDbContext(optionsBuilder.Options);
    }
}
