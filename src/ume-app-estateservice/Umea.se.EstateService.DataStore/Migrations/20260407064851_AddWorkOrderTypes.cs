using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkOrderTypes",
                table: "Buildings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkOrderTypes",
                table: "Buildings");
        }
    }
}
