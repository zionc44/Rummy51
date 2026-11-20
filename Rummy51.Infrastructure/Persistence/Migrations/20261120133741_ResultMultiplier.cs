using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rummy51.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResultMultiplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "doubled",
                table: "hand_results",
                newName: "result_multiplier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "result_multiplier",
                table: "hand_results",
                newName: "doubled");
        }
    }
}
