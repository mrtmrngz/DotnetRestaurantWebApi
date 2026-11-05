using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSystemRoleFieldForAppRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "AspNetRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "AspNetRoles");
        }
    }
}
