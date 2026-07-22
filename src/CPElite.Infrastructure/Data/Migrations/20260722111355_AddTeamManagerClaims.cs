using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamManagerClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamManagerClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedRole = table.Column<int>(type: "integer", nullable: false),
                    ApprovalThreshold = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamManagerClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamManagerClaims_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamManagerClaims_Users_ClaimantUserId",
                        column: x => x.ClaimantUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamManagerClaimVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamManagerClaimVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamManagerClaimVotes_TeamManagerClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "TeamManagerClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamManagerClaimVotes_Users_VoterUserId",
                        column: x => x.VoterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamManagerClaims_ClaimantUserId",
                table: "TeamManagerClaims",
                column: "ClaimantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamManagerClaims_TeamId_ClaimantUserId_Status",
                table: "TeamManagerClaims",
                columns: new[] { "TeamId", "ClaimantUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamManagerClaimVotes_ClaimId_VoterUserId",
                table: "TeamManagerClaimVotes",
                columns: new[] { "ClaimId", "VoterUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamManagerClaimVotes_TeamId_VoterUserId",
                table: "TeamManagerClaimVotes",
                columns: new[] { "TeamId", "VoterUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamManagerClaimVotes_VoterUserId",
                table: "TeamManagerClaimVotes",
                column: "VoterUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamManagerClaimVotes");

            migrationBuilder.DropTable(
                name: "TeamManagerClaims");
        }
    }
}
