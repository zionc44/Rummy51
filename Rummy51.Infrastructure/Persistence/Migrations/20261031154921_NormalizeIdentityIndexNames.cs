using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rummy51.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeIdentityIndexNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "players");

            migrationBuilder.RenameIndex(
                name: "RoleNameIndex",
                table: "roles",
                newName: "ix_roles_normalized_name");

            migrationBuilder.RenameIndex(
                name: "UserNameIndex",
                table: "players",
                newName: "ix_players_normalized_user_name");

            migrationBuilder.CreateIndex(
                name: "ix_players_normalized_email",
                table: "players",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_players_normalized_email",
                table: "players");

            migrationBuilder.RenameIndex(
                name: "ix_roles_normalized_name",
                table: "roles",
                newName: "RoleNameIndex");

            migrationBuilder.RenameIndex(
                name: "ix_players_normalized_user_name",
                table: "players",
                newName: "UserNameIndex");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "players",
                column: "normalized_email");
        }
    }
}
