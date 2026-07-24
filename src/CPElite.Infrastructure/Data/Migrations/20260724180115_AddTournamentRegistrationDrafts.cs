using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentRegistrationDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentRegistrationDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Step = table.Column<int>(type: "integer", nullable: false),
                    PaymentMode = table.Column<int>(type: "integer", nullable: false),
                    RulesAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    Formation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SelectedPlayersJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRegistrationDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationDrafts_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationDrafts_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationDrafts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationDrafts_TeamId",
                table: "TournamentRegistrationDrafts",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationDrafts_TournamentId_TeamId_UserId",
                table: "TournamentRegistrationDrafts",
                columns: new[] { "TournamentId", "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationDrafts_TournamentId_UserId_UpdatedAt",
                table: "TournamentRegistrationDrafts",
                columns: new[] { "TournamentId", "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationDrafts_UserId",
                table: "TournamentRegistrationDrafts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentRegistrationDrafts");
        }
    }
}
