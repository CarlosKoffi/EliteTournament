using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPElite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizedContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalizedContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Section = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalizedContents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalizedContents_Key_Language",
                table: "LocalizedContents",
                columns: new[] { "Key", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalizedContents_Language_Section",
                table: "LocalizedContents",
                columns: new[] { "Language", "Section" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalizedContents");
        }
    }
}
