using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEaIdentityVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EaIdentityVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EaIdentityVerifiedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EaIdentityVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EaIdentityVerifiedAt",
                table: "Users");
        }
    }
}
