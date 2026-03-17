using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations;

/// <inheritdoc />
public partial class AddBuildingBackgroundCache : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "NumDocuments",
            table: "Buildings",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "BackgroundCacheFetchedAtUtc",
            table: "Buildings",
            type: "datetimeoffset",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "NumDocuments",
            table: "Buildings");

        migrationBuilder.DropColumn(
            name: "BackgroundCacheFetchedAtUtc",
            table: "Buildings");
    }
}
