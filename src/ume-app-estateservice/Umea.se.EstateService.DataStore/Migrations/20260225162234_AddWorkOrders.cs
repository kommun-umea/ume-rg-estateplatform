using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations;

/// <inheritdoc />
public partial class AddWorkOrders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "WorkOrderCategoriesJson",
            table: "SyncMetadata",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Rooms",
            type: "datetimeoffset",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Floors",
            type: "datetimeoffset",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Estates",
            type: "datetimeoffset",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Buildings",
            type: "datetimeoffset",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        migrationBuilder.CreateTable(
            name: "WorkOrders",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BuildingId = table.Column<int>(type: "int", nullable: false),
                BuildingName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RoomId = table.Column<int>(type: "int", nullable: true),
                RoomName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Location = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                WorkOrderTypeId = table.Column<int>(type: "int", nullable: false),
                PythagorasWorkOrderId = table.Column<int>(type: "int", nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                RetryCount = table.Column<int>(type: "int", nullable: false),
                NextSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedByEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                NotifierEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                NotifierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkOrders", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WorkOrderFiles",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                WorkOrderId = table.Column<int>(type: "int", nullable: false),
                FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                FileSize = table.Column<long>(type: "bigint", nullable: false),
                StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Uploaded = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkOrderFiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkOrderFiles_WorkOrders_WorkOrderId",
                    column: x => x.WorkOrderId,
                    principalTable: "WorkOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrderFiles_WorkOrderId",
            table: "WorkOrderFiles",
            column: "WorkOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrders_BuildingId",
            table: "WorkOrders",
            column: "BuildingId");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrders_NextSyncAt",
            table: "WorkOrders",
            column: "NextSyncAt");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrders_Status",
            table: "WorkOrders",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_WorkOrders_Uid",
            table: "WorkOrders",
            column: "Uid");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WorkOrderFiles");

        migrationBuilder.DropTable(
            name: "WorkOrders");

        migrationBuilder.DropColumn(
            name: "WorkOrderCategoriesJson",
            table: "SyncMetadata");

        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "Rooms");

        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "Floors");

        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "Estates");

        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "Buildings");
    }
}
