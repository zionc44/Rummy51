using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rummy51.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GameStanding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "hands_played",
                table: "game_players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "out_after_hand",
                table: "game_players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "place",
                table: "game_players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total",
                table: "game_players",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hands_played",
                table: "game_players");

            migrationBuilder.DropColumn(
                name: "out_after_hand",
                table: "game_players");

            migrationBuilder.DropColumn(
                name: "place",
                table: "game_players");

            migrationBuilder.DropColumn(
                name: "total",
                table: "game_players");
        }
    }
}
