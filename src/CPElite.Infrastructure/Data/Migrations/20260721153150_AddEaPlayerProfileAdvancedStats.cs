using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEaPlayerProfileAdvancedStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllClubsAssists",
                table: "EaPlayerProfileSnapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AllClubsAverageRating",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AllClubsGoals",
                table: "EaPlayerProfileSnapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AllClubsMatches",
                table: "EaPlayerProfileSnapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AllClubsPlayerOfTheMatch",
                table: "EaPlayerProfileSnapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoredAllClubsGoalContributions",
                table: "EaPlayerProfileSnapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredAllClubsGoalContributionsPerMatch",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredAllClubsPlayerOfTheMatchRate",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredAssistsPerMatch",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredCleanSheetsDefRate",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredCleanSheetsGkRate",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoredGoalContributions",
                table: "EaPlayerProfileSnapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredGoalContributionsPerMatch",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredGoalsPerMatch",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredPassesMadePerMatch",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredPlayerOfTheMatchRate",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StoredTacklesMadePerMatch",
                table: "EaPlayerProfileSnapshots",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllClubsAssists",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "AllClubsAverageRating",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "AllClubsGoals",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "AllClubsMatches",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "AllClubsPlayerOfTheMatch",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredAllClubsGoalContributions",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredAllClubsGoalContributionsPerMatch",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredAllClubsPlayerOfTheMatchRate",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredAssistsPerMatch",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredCleanSheetsDefRate",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredCleanSheetsGkRate",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredGoalContributions",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredGoalContributionsPerMatch",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredGoalsPerMatch",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredPassesMadePerMatch",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredPlayerOfTheMatchRate",
                table: "EaPlayerProfileSnapshots");

            migrationBuilder.DropColumn(
                name: "StoredTacklesMadePerMatch",
                table: "EaPlayerProfileSnapshots");
        }
    }
}
