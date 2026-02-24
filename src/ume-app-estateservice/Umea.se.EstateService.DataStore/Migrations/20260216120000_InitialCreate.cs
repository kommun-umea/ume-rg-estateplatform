using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingAscendants",
                columns: table => new
                {
                    BuildingId = table.Column<int>(type: "int", nullable: false),
                    EstateAscendantId = table.Column<int>(type: "int", nullable: true),
                    EstateAscendantName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstateAscendantPopularName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstateAscendantGeoLat = table.Column<double>(type: "float", nullable: true),
                    EstateAscendantGeoLon = table.Column<double>(type: "float", nullable: true),
                    RegionAscendantId = table.Column<int>(type: "int", nullable: true),
                    RegionAscendantName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegionAscendantPopularName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegionAscendantGeoLat = table.Column<double>(type: "float", nullable: true),
                    RegionAscendantGeoLon = table.Column<double>(type: "float", nullable: true),
                    OrganizationAscendantId = table.Column<int>(type: "int", nullable: true),
                    OrganizationAscendantName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrganizationAscendantPopularName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrganizationAscendantGeoLat = table.Column<double>(type: "float", nullable: true),
                    OrganizationAscendantGeoLon = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingAscendants", x => x.BuildingId);
                });

            migrationBuilder.CreateTable(
                name: "BusinessTypeModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessTypeModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Estates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    GrossArea = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetArea = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    GeoLocationLat = table.Column<double>(type: "float", nullable: true),
                    GeoLocationLon = table.Column<double>(type: "float", nullable: true),
                    AddressStreet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddressZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressExtra = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PropertyDesignation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OperationalArea = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AdministrativeArea = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MunicipalityArea = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalOwnerStatus = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalOwnerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExternalOwnerNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BuildingCount = table.Column<int>(type: "int", nullable: false),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PopularName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    GrossArea = table.Column<double>(type: "float", nullable: true),
                    NetArea = table.Column<double>(type: "float", nullable: true),
                    Height = table.Column<double>(type: "float", nullable: true),
                    BuildingId = table.Column<int>(type: "int", nullable: false),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PopularName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    GrossArea = table.Column<double>(type: "float", nullable: false),
                    NetArea = table.Column<double>(type: "float", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    BuildingId = table.Column<int>(type: "int", nullable: false),
                    FloorId = table.Column<int>(type: "int", nullable: true),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PopularName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    LastRefreshUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EstateCount = table.Column<int>(type: "int", nullable: false),
                    BuildingCount = table.Column<int>(type: "int", nullable: false),
                    FloorCount = table.Column<int>(type: "int", nullable: false),
                    RoomCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    AddressStreet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddressZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressExtra = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GeoLocationLat = table.Column<double>(type: "float", nullable: true),
                    GeoLocationLon = table.Column<double>(type: "float", nullable: true),
                    GrossArea = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetArea = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    YearOfConstruction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuildingCondition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalOwnerStatus = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalOwnerName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExternalOwnerNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PropertyDesignation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NoticeBoardText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NoticeBoardStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoticeBoardEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlueprintAvailable = table.Column<bool>(type: "bit", nullable: true),
                    PropertyManager = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OperationsManager = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OperationCoordinator = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RentalAdministrator = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessTypeId = table.Column<int>(type: "int", nullable: true),
                    NumFloors = table.Column<int>(type: "int", nullable: false),
                    NumRooms = table.Column<int>(type: "int", nullable: false),
                    ImageIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EstateId = table.Column<int>(type: "int", nullable: false),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PopularName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buildings_BusinessTypeModel_BusinessTypeId",
                        column: x => x.BusinessTypeId,
                        principalTable: "BusinessTypeModel",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_BusinessTypeId",
                table: "Buildings",
                column: "BusinessTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_EstateId",
                table: "Buildings",
                column: "EstateId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_Name",
                table: "Buildings",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_Uid",
                table: "Buildings",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_Estates_Name",
                table: "Estates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Estates_Uid",
                table: "Estates",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_BuildingId",
                table: "Floors",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_Uid",
                table: "Floors",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_BuildingId",
                table: "Rooms",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_FloorId",
                table: "Rooms",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Uid",
                table: "Rooms",
                column: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BuildingAscendants");
            migrationBuilder.DropTable(name: "Buildings");
            migrationBuilder.DropTable(name: "Estates");
            migrationBuilder.DropTable(name: "Floors");
            migrationBuilder.DropTable(name: "Rooms");
            migrationBuilder.DropTable(name: "SyncMetadata");
            migrationBuilder.DropTable(name: "BusinessTypeModel");
        }
    }
}
