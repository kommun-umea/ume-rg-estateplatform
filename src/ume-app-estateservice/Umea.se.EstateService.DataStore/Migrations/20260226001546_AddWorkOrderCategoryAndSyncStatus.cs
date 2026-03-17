using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations;

/// <inheritdoc />
public partial class AddWorkOrderCategoryAndSyncStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Status",
            table: "WorkOrders",
            newName: "SyncStatus");

        migrationBuilder.RenameIndex(
            name: "IX_WorkOrders_Status",
            table: "WorkOrders",
            newName: "IX_WorkOrders_SyncStatus");

        migrationBuilder.AddColumn<int>(
            name: "CategoryId",
            table: "WorkOrders",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PythagorasStatusId",
            table: "WorkOrders",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PythagorasStatusName",
            table: "WorkOrders",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CategoryId",
            table: "WorkOrders");

        migrationBuilder.DropColumn(
            name: "PythagorasStatusId",
            table: "WorkOrders");

        migrationBuilder.DropColumn(
            name: "PythagorasStatusName",
            table: "WorkOrders");

        migrationBuilder.RenameColumn(
            name: "SyncStatus",
            table: "WorkOrders",
            newName: "Status");

        migrationBuilder.RenameIndex(
            name: "IX_WorkOrders_SyncStatus",
            table: "WorkOrders",
            newName: "IX_WorkOrders_Status");
    }
}
