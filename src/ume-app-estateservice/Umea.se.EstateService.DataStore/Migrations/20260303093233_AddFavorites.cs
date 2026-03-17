using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations;

/// <inheritdoc />
public partial class AddFavorites : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Favorites",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                NodeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                NodeId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Favorites", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Favorites_UserEmail",
            table: "Favorites",
            column: "UserEmail");

        migrationBuilder.CreateIndex(
            name: "IX_Favorites_UserEmail_NodeType_NodeId",
            table: "Favorites",
            columns: new[] { "UserEmail", "NodeType", "NodeId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Favorites");
    }
}
