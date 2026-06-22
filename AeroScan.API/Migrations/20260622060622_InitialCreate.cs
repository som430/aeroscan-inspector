using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroScan.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspectionSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PartNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    PointCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ToleranceMm = table.Column<double>(type: "REAL", nullable: false),
                    Passed = table.Column<bool>(type: "INTEGER", nullable: true),
                    FlatnessErrorMm = table.Column<double>(type: "REAL", nullable: true),
                    MinDeviationMm = table.Column<double>(type: "REAL", nullable: true),
                    MaxDeviationMm = table.Column<double>(type: "REAL", nullable: true),
                    RmsDeviationMm = table.Column<double>(type: "REAL", nullable: true),
                    OutOfToleranceCount = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    DeviationSnapshotJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionSessions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspectionSessions");
        }
    }
}
