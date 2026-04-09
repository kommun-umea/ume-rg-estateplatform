using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentSyncFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DocumentLatestUpdatedEpoch",
                table: "SyncMetadata",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentTotalCount",
                table: "SyncMetadata",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentLatestUpdatedEpoch",
                table: "SyncMetadata");

            migrationBuilder.DropColumn(
                name: "DocumentTotalCount",
                table: "SyncMetadata");
        }
    }
}
