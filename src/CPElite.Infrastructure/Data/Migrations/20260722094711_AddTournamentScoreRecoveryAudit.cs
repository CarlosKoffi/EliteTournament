using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentScoreRecoveryAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPublishPerfectScore",
                table: "Tournaments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ScoreRecoveryIntervalMinutes",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "ScoreRecoveryMode",
                table: "Tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "TournamentScoreAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentMatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Trigger = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EaMatchId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CandidateCount = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: true),
                    HomeScore = table.Column<int>(type: "integer", nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: true),
                    ManualHomeScore = table.Column<int>(type: "integer", nullable: true),
                    ManualAwayScore = table.Column<int>(type: "integer", nullable: true),
                    TeamsMatched = table.Column<bool>(type: "boolean", nullable: false),
                    TimestampMatched = table.Column<bool>(type: "boolean", nullable: false),
                    ScoreMatched = table.Column<bool>(type: "boolean", nullable: false),
                    PlayersMatched = table.Column<bool>(type: "boolean", nullable: false),
                    DurationLooksComplete = table.Column<bool>(type: "boolean", nullable: false),
                    DuplicateCandidateDetected = table.Column<bool>(type: "boolean", nullable: false),
                    IssuesJson = table.Column<string>(type: "jsonb", nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    RawCandidateJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentScoreAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentScoreAudits_TournamentMatches_TournamentMatchId",
                        column: x => x.TournamentMatchId,
                        principalTable: "TournamentMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentScoreAudits_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentScoreAudits_Status",
                table: "TournamentScoreAudits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentScoreAudits_TournamentId_AttemptedAt",
                table: "TournamentScoreAudits",
                columns: new[] { "TournamentId", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentScoreAudits_TournamentMatchId_AttemptedAt",
                table: "TournamentScoreAudits",
                columns: new[] { "TournamentMatchId", "AttemptedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentScoreAudits");

            migrationBuilder.DropColumn(
                name: "AutoPublishPerfectScore",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "ScoreRecoveryIntervalMinutes",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "ScoreRecoveryMode",
                table: "Tournaments");
        }
    }
}
