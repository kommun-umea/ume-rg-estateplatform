using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Umea.se.EstateService.DataStore.Migrations;

/// <inheritdoc />
public partial class AddFavoriteNodeTypeCheckConstraint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "CK_Favorites_NodeType",
            table: "Favorites",
            sql: "[NodeType] IN ('Estate', 'Building', 'Room')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Favorites_NodeType",
            table: "Favorites");
    }
}
