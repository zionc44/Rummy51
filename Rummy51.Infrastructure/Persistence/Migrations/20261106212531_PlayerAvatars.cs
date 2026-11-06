using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rummy51.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlayerAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar_image_tag",
                table: "players",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "avatar_source",
                table: "players",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Provider");

            migrationBuilder.CreateTable(
                name: "player_avatars",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image = table.Column<byte[]>(type: "bytea", nullable: false),
                    content_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_avatars", x => x.player_id);
                    table.ForeignKey(
                        name: "fk_player_avatars_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_avatars");

            migrationBuilder.DropColumn(
                name: "avatar_image_tag",
                table: "players");

            migrationBuilder.DropColumn(
                name: "avatar_source",
                table: "players");
        }
    }
}
