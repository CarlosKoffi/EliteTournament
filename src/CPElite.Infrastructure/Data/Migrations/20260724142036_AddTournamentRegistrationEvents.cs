using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentRegistrationEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentRegistrationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentRegistrationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Step = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RegistrationStatus = table.Column<int>(type: "integer", nullable: true),
                    PaymentMode = table.Column<int>(type: "integer", nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRegistrationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationEvents_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationEvents_TournamentRegistrations_Tourna~",
                        column: x => x.TournamentRegistrationId,
                        principalTable: "TournamentRegistrations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationEvents_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrationEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationEvents_ActorUserId",
                table: "TournamentRegistrationEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationEvents_EventType_CreatedAt",
                table: "TournamentRegistrationEvents",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationEvents_TeamId",
                table: "TournamentRegistrationEvents",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationEvents_TournamentId_TeamId_CreatedAt",
                table: "TournamentRegistrationEvents",
                columns: new[] { "TournamentId", "TeamId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrationEvents_TournamentRegistrationId",
                table: "TournamentRegistrationEvents",
                column: "TournamentRegistrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentRegistrationEvents");
        }
    }
}
