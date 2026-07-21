using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEaMatchClubDetailStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CrestAssetId",
                table: "EaMatchClubStats",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EaTeamId",
                table: "EaMatchClubStats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KitColor1",
                table: "EaMatchClubStats",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KitColor2",
                table: "EaMatchClubStats",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KitColor3",
                table: "EaMatchClubStats",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KitColor4",
                table: "EaMatchClubStats",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Losses",
                table: "EaMatchClubStats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "EaMatchClubStats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "EaMatchClubStats",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "EaMatchClubStats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StadiumName",
                table: "EaMatchClubStats",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Ties",
                table: "EaMatchClubStats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WinnerByDnf",
                table: "EaMatchClubStats",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Wins",
                table: "EaMatchClubStats",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrestAssetId",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "EaTeamId",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "KitColor1",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "KitColor2",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "KitColor3",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "KitColor4",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "Losses",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "StadiumName",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "Ties",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "WinnerByDnf",
                table: "EaMatchClubStats");

            migrationBuilder.DropColumn(
                name: "Wins",
                table: "EaMatchClubStats");
        }
    }
}
