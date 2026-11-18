using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rummy51.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GamesAndResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    target = table.Column<int>(type: "integer", nullable: true),
                    seats = table.Column<int>(type: "integer", nullable: false),
                    jokers = table.Column<int>(type: "integer", nullable: false),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_games", x => x.id);
                    table.ForeignKey(
                        name: "fk_games_players_winner_id",
                        column: x => x.winner_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_players",
                columns: table => new
                {
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_players", x => new { x.game_id, x.player_id });
                    table.ForeignKey(
                        name: "fk_game_players_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_game_players_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "played_hands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    dealer_seat = table.Column<int>(type: "integer", nullable: false),
                    cut_at = table.Column<int>(type: "integer", nullable: false),
                    finishing_card = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    seed = table.Column<int>(type: "integer", nullable: false),
                    deck_order = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    agreed_redeals = table.Column<int>(type: "integer", nullable: false),
                    forced_redeals = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_played_hands", x => x.id);
                    table.ForeignKey(
                        name: "fk_played_hands_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hand_results",
                columns: table => new
                {
                    played_hand_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    won = table.Column<bool>(type: "boolean", nullable: false),
                    went_down = table.Column<bool>(type: "boolean", nullable: false),
                    went_rummy = table.Column<bool>(type: "boolean", nullable: false),
                    took_finishing_card = table.Column<bool>(type: "boolean", nullable: false),
                    walked_away = table.Column<bool>(type: "boolean", nullable: false),
                    cards_left = table.Column<int>(type: "integer", nullable: false),
                    held_cards = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    held_worth = table.Column<int>(type: "integer", nullable: false),
                    doubled = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hand_results", x => new { x.played_hand_id, x.player_id });
                    table.ForeignKey(
                        name: "fk_hand_results_played_hands_played_hand_id",
                        column: x => x.played_hand_id,
                        principalTable: "played_hands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_hand_results_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_players_game_id_seat",
                table: "game_players",
                columns: new[] { "game_id", "seat" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_players_player_id",
                table: "game_players",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_games_started_at",
                table: "games",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_games_winner_id",
                table: "games",
                column: "winner_id");

            migrationBuilder.CreateIndex(
                name: "ix_hand_results_player_id",
                table: "hand_results",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_played_hands_game_id_number",
                table: "played_hands",
                columns: new[] { "game_id", "number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_players");

            migrationBuilder.DropTable(
                name: "hand_results");

            migrationBuilder.DropTable(
                name: "played_hands");

            migrationBuilder.DropTable(
                name: "games");
        }
    }
}
