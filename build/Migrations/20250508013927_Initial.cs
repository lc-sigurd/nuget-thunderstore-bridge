using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    PackageUuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Namespace = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.PackageUuid);
                });

            migrationBuilder.CreateTable(
                name: "PackageVersions",
                columns: table => new
                {
                    PackageVersionUuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumberString = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PackageUuid = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageVersions", x => x.PackageVersionUuid);
                    table.ForeignKey(
                        name: "FK_PackageVersions_Packages_PackageUuid",
                        column: x => x.PackageUuid,
                        principalTable: "Packages",
                        principalColumn: "PackageUuid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Name",
                table: "Packages",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Namespace",
                table: "Packages",
                column: "Namespace");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Namespace_Name",
                table: "Packages",
                columns: new[] { "Namespace", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageVersions_PackageUuid",
                table: "PackageVersions",
                column: "PackageUuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageVersions");

            migrationBuilder.DropTable(
                name: "Packages");
        }
    }
}
