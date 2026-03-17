using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Umea.se.EstateService.DataStore.Entities;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.DataStore;

/// <summary>
/// EF Core DbContext for the Estate Service database.
/// Uses domain entities directly with owned types for value objects.
/// </summary>
public class EstateDbContext(DbContextOptions<EstateDbContext> options) : DbContext(options)
{
    public DbSet<EstateEntity> Estates => Set<EstateEntity>();
    public DbSet<BuildingEntity> Buildings => Set<BuildingEntity>();
    public DbSet<FloorEntity> Floors => Set<FloorEntity>();
    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();
    public DbSet<BuildingAscendantDbEntity> BuildingAscendants => Set<BuildingAscendantDbEntity>();
    public DbSet<DataSyncMetadata> SyncMetadata => Set<DataSyncMetadata>();
    public DbSet<WorkOrderEntity> WorkOrders => Set<WorkOrderEntity>();
    public DbSet<WorkOrderFileEntity> WorkOrderFiles => Set<WorkOrderFileEntity>();
    public DbSet<FavoriteEntity> Favorites => Set<FavoriteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEstate(modelBuilder);
        ConfigureBuilding(modelBuilder);
        ConfigureFloor(modelBuilder);
        ConfigureRoom(modelBuilder);
        ConfigureBuildingAscendant(modelBuilder);
        ConfigureSyncMetadata(modelBuilder);
        ConfigureWorkOrder(modelBuilder);
        ConfigureWorkOrderFile(modelBuilder);
        ConfigureFavorite(modelBuilder);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            ApplySqliteDateTimeOffsetConversions(modelBuilder);
        }
    }

    /// <summary>
    /// SQLite does not natively support DateTimeOffset in ORDER BY.
    /// This converts DateTimeOffset properties to ticks for storage, enabling sorting.
    /// Only applied when running under SQLite (tests).
    /// </summary>
    private static void ApplySqliteDateTimeOffsetConversions(ModelBuilder modelBuilder)
    {
        ValueConverter<DateTimeOffset, long> dateTimeOffsetConverter = new(
            v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        ValueConverter<DateTimeOffset?, long?> nullableDateTimeOffsetConverter = new(
            v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : null,
            v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(dateTimeOffsetConverter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableDateTimeOffsetConverter);
                }
            }
        }
    }

    private static void ConfigureEstate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EstateEntity>(entity =>
        {
            entity.ToTable("Estates");

            entity.HasKey(e => e.Id);

            // Id is not auto-generated - comes from Pythagoras
            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.PopularName)
                .HasMaxLength(500);

            entity.Property(e => e.GrossArea)
                .HasPrecision(18, 4);

            entity.Property(e => e.NetArea)
                .HasPrecision(18, 4);

            // Extended properties
            entity.Property(e => e.PropertyDesignation).HasMaxLength(200);
            entity.Property(e => e.OperationalArea).HasMaxLength(200);
            entity.Property(e => e.AdministrativeArea).HasMaxLength(200);
            entity.Property(e => e.MunicipalityArea).HasMaxLength(200);
            entity.Property(e => e.ExternalOwnerStatus).HasMaxLength(200);
            entity.Property(e => e.ExternalOwnerName).HasMaxLength(500);
            entity.Property(e => e.ExternalOwnerNote).HasMaxLength(2000);

            // Configure Address as owned type
            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.Street).HasColumnName("AddressStreet").HasMaxLength(500);
                address.Property(a => a.ZipCode).HasColumnName("AddressZipCode").HasMaxLength(20);
                address.Property(a => a.City).HasColumnName("AddressCity").HasMaxLength(200);
                address.Property(a => a.Country).HasColumnName("AddressCountry").HasMaxLength(100);
                address.Property(a => a.Extra).HasColumnName("AddressExtra").HasMaxLength(500);
            });

            // Configure GeoLocation as owned type
            entity.OwnsOne(e => e.GeoLocation, geo =>
            {
                geo.Property(g => g.Lat).HasColumnName("GeoLocationLat");
                geo.Property(g => g.Lon).HasColumnName("GeoLocationLon");
            });

            // Ignore navigation property for EF mapping
            entity.Ignore(e => e.Buildings);
            entity.Ignore(e => e.ParentId);

            entity.HasIndex(e => e.Uid);
            entity.HasIndex(e => e.Name);
        });
    }

    private static void ConfigureBuilding(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BuildingEntity>(entity =>
        {
            entity.ToTable("Buildings");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.PopularName)
                .HasMaxLength(500);

            entity.Property(e => e.GrossArea)
                .HasPrecision(18, 4);

            entity.Property(e => e.NetArea)
                .HasPrecision(18, 4);

            // Building properties
            entity.Property(e => e.YearOfConstruction).HasMaxLength(50);
            entity.Property(e => e.BuildingCondition).HasMaxLength(200);
            entity.Property(e => e.ExternalOwnerStatus).HasMaxLength(200);
            entity.Property(e => e.ExternalOwnerName).HasMaxLength(500);
            entity.Property(e => e.ExternalOwnerNote).HasMaxLength(2000);
            entity.Property(e => e.PropertyDesignation).HasMaxLength(200);

            // Configure Address as owned type
            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.Street).HasColumnName("AddressStreet").HasMaxLength(500);
                address.Property(a => a.ZipCode).HasColumnName("AddressZipCode").HasMaxLength(20);
                address.Property(a => a.City).HasColumnName("AddressCity").HasMaxLength(200);
                address.Property(a => a.Country).HasColumnName("AddressCountry").HasMaxLength(100);
                address.Property(a => a.Extra).HasColumnName("AddressExtra").HasMaxLength(500);
            });

            // Configure GeoLocation as owned type
            entity.OwnsOne(e => e.GeoLocation, geo =>
            {
                geo.Property(g => g.Lat).HasColumnName("GeoLocationLat");
                geo.Property(g => g.Lon).HasColumnName("GeoLocationLon");
            });

            // Configure NoticeBoard as owned type
            entity.OwnsOne(e => e.NoticeBoard, nb =>
            {
                nb.Property(n => n.Text).HasColumnName("NoticeBoardText").HasMaxLength(4000).IsRequired();
                nb.Property(n => n.StartDate).HasColumnName("NoticeBoardStartDate");
                nb.Property(n => n.EndDate).HasColumnName("NoticeBoardEndDate");
            });

            // Configure ContactPersons as owned type
            entity.OwnsOne(e => e.ContactPersons, cp =>
            {
                cp.Property(c => c.PropertyManager).HasColumnName("PropertyManager").HasMaxLength(500).IsRequired();
                cp.Property(c => c.OperationsManager).HasColumnName("OperationsManager").HasMaxLength(500);
                cp.Property(c => c.OperationCoordinator).HasColumnName("OperationCoordinator").HasMaxLength(500);
                cp.Property(c => c.RentalAdministrator).HasColumnName("RentalAdministrator").HasMaxLength(500);
            });

            // Configure BusinessType as owned type
            entity.OwnsOne(e => e.BusinessType, bt =>
            {
                bt.Property(b => b.Id).HasColumnName("BusinessTypeId").ValueGeneratedNever();
                bt.Property(b => b.Name).HasColumnName("BusinessTypeName").HasMaxLength(500);
            });

            // Configure ImageIds as JSON column
            ValueConverter<IReadOnlyList<int>?, string?> imageIdsConverter = new(
                v => v == null || v.Count == 0 ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions?)null));

            ValueComparer<IReadOnlyList<int>?> imageIdsComparer = new(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v)),
                c => c == null ? null : (IReadOnlyList<int>)c.ToList());

            entity.Property(e => e.ImageIds)
                .HasConversion(imageIdsConverter)
                .Metadata.SetValueComparer(imageIdsComparer);

            entity.Property(e => e.ImageIds)
                .HasColumnName("ImageIds")
                .HasMaxLength(4000);

            entity.Property(e => e.NumDocuments)
                .HasColumnName("NumDocuments");

            entity.Property(e => e.BackgroundCacheFetchedAtUtc)
                .HasColumnName("BackgroundCacheFetchedAtUtc");

            // Ignore navigation properties for EF mapping
            entity.Ignore(e => e.Floors);
            entity.Ignore(e => e.Rooms);
            entity.Ignore(e => e.ParentId);

            entity.HasIndex(e => e.Uid);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.EstateId);
        });
    }

    private static void ConfigureFloor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FloorEntity>(entity =>
        {
            entity.ToTable("Floors");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.PopularName)
                .HasMaxLength(500);

            // Ignore navigation properties
            entity.Ignore(e => e.Rooms);
            entity.Ignore(e => e.ParentId);

            entity.HasIndex(e => e.Uid);
            entity.HasIndex(e => e.BuildingId);
        });
    }

    private static void ConfigureRoom(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomEntity>(entity =>
        {
            entity.ToTable("Rooms");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.PopularName)
                .HasMaxLength(500);

            // Ignore navigation property
            entity.Ignore(e => e.ParentId);

            entity.HasIndex(e => e.Uid);
            entity.HasIndex(e => e.BuildingId);
            entity.HasIndex(e => e.FloorId);
        });
    }

    private static void ConfigureBuildingAscendant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BuildingAscendantDbEntity>(entity =>
        {
            entity.ToTable("BuildingAscendants");

            entity.HasKey(e => e.BuildingId);

            // BuildingId comes from Pythagoras, not auto-generated
            entity.Property(e => e.BuildingId)
                .ValueGeneratedNever();

            // Estate ascendant fields
            entity.Property(e => e.EstateAscendantName).HasMaxLength(500);
            entity.Property(e => e.EstateAscendantPopularName).HasMaxLength(500);

            // Region ascendant fields
            entity.Property(e => e.RegionAscendantName).HasMaxLength(500);
            entity.Property(e => e.RegionAscendantPopularName).HasMaxLength(500);

            // Organization ascendant fields
            entity.Property(e => e.OrganizationAscendantName).HasMaxLength(500);
            entity.Property(e => e.OrganizationAscendantPopularName).HasMaxLength(500);

            // Remove navigation property configuration - we'll handle relationships manually
            entity.Ignore(e => e.Building);
        });
    }

    private static void ConfigureSyncMetadata(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataSyncMetadata>(entity =>
        {
            entity.ToTable("SyncMetadata");
            entity.HasKey(e => e.Id);

            // Id is explicitly set, not auto-generated
            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.WorkOrderCategoriesJson);
        });
    }

    private static void ConfigureWorkOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrderEntity>(entity =>
        {
            entity.ToTable("WorkOrders");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Location)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.SyncStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("SyncStatus")
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.BuildingName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.RoomName)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedByEmail)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.NotifierEmail)
                .HasMaxLength(200);

            entity.Property(e => e.NotifierName)
                .HasMaxLength(200);

            entity.Property(e => e.PythagorasStatusName)
                .HasMaxLength(200);

            entity.HasMany(e => e.Files)
                .WithOne(f => f.WorkOrder)
                .HasForeignKey(f => f.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.NextSyncAt);
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.BuildingId);
            entity.HasIndex(e => e.Uid);
        });
    }

    private static void ConfigureWorkOrderFile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrderFileEntity>(entity =>
        {
            entity.ToTable("WorkOrderFiles");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.ContentType)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.StoragePath)
                .HasMaxLength(1000)
                .IsRequired();

            entity.HasIndex(e => e.WorkOrderId);
        });
    }

    private static void ConfigureFavorite(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FavoriteEntity>(entity =>
        {
            entity.ToTable("Favorites", t => t.HasCheckConstraint(
                "CK_Favorites_NodeType",
                "[NodeType] IN ('Estate', 'Building', 'Room')"));

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserEmail)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.NodeType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.UserEmail);

            entity.HasIndex(e => new { e.UserEmail, e.NodeType, e.NodeId })
                .IsUnique();
        });
    }
}
