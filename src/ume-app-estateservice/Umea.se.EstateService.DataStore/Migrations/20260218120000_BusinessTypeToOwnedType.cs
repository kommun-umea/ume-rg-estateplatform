using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations;

/// <inheritdoc />
public partial class BusinessTypeToOwnedType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop the FK and index from Buildings to BusinessTypeModel
        migrationBuilder.DropForeignKey(
            name: "FK_Buildings_BusinessTypeModel_BusinessTypeId",
            table: "Buildings");

        migrationBuilder.DropIndex(
            name: "IX_Buildings_BusinessTypeId",
            table: "Buildings");

        // Drop the standalone BusinessTypeModel table
        migrationBuilder.DropTable(
            name: "BusinessTypeModel");

        // Add BusinessTypeName column to Buildings (BusinessTypeId already exists)
        migrationBuilder.AddColumn<string>(
            name: "BusinessTypeName",
            table: "Buildings",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        // Migrate existing data: copy Name from the now-dropped table is not possible,
        // but the data sync will repopulate on next refresh.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Recreate the BusinessTypeModel table
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

        // Remove the BusinessTypeName column
        migrationBuilder.DropColumn(
            name: "BusinessTypeName",
            table: "Buildings");

        // Re-create the FK and index
        migrationBuilder.CreateIndex(
            name: "IX_Buildings_BusinessTypeId",
            table: "Buildings",
            column: "BusinessTypeId");

        migrationBuilder.AddForeignKey(
            name: "FK_Buildings_BusinessTypeModel_BusinessTypeId",
            table: "Buildings",
            column: "BusinessTypeId",
            principalTable: "BusinessTypeModel",
            principalColumn: "Id");
    }
}
