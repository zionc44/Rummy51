using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rummy51.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedealsAndAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hand_redeals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    played_hand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    offered_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    forced = table.Column<bool>(type: "boolean", nullable: false),
                    at_once = table.Column<bool>(type: "boolean", nullable: false),
                    forced_with = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    agreed = table.Column<bool>(type: "boolean", nullable: false),
                    asked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_redeals", x => x.id);
                    table.ForeignKey(
                        name: "fk_hand_redeals_played_hands_played_hand_id",
                        column: x => x.played_hand_id,
                        principalTable: "played_hands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_hand_redeals_players_offered_by_id",
                        column: x => x.offered_by_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "redeal_answers",
                columns: table => new
                {
                    hand_redeal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reply = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_redeal_answers", x => new { x.hand_redeal_id, x.player_id });
                    table.ForeignKey(
                        name: "fk_redeal_answers_hand_redeals_hand_redeal_id",
                        column: x => x.hand_redeal_id,
                        principalTable: "hand_redeals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_redeal_answers_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hand_redeals_offered_by_id",
                table: "hand_redeals",
                column: "offered_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_hand_redeals_played_hand_id_number",
                table: "hand_redeals",
                columns: new[] { "played_hand_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_redeal_answers_player_id",
                table: "redeal_answers",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redeal_answers");

            migrationBuilder.DropTable(
                name: "hand_redeals");
        }
    }
}
