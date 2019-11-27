using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ISQExplorer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Professors",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    NNumber = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Professors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Queries",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseCode = table.Column<string>(nullable: true),
                    CourseName = table.Column<string>(nullable: true),
                    ProfessorName = table.Column<string>(nullable: true),
                    SeasonSince = table.Column<int>(nullable: true),
                    YearSince = table.Column<int>(nullable: true),
                    SeasonUntil = table.Column<int>(nullable: true),
                    YearUntil = table.Column<int>(nullable: true),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseCodes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(nullable: false),
                    CourseCode = table.Column<string>(maxLength: 12, nullable: false),
                    Season = table.Column<int>(nullable: true),
                    Year = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseCodes_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseNames",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    Season = table.Column<int>(nullable: true),
                    Year = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseNames_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsqEntries",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(nullable: true),
                    Season = table.Column<int>(nullable: false),
                    Year = table.Column<int>(nullable: false),
                    ProfessorId = table.Column<int>(nullable: true),
                    Crn = table.Column<int>(nullable: false),
                    NResponded = table.Column<int>(nullable: false),
                    NTotal = table.Column<int>(nullable: false),
                    Pct5 = table.Column<double>(nullable: false),
                    Pct4 = table.Column<double>(nullable: false),
                    Pct3 = table.Column<double>(nullable: false),
                    Pct2 = table.Column<double>(nullable: false),
                    Pct1 = table.Column<double>(nullable: false),
                    PctNa = table.Column<double>(nullable: false),
                    NEnrolled = table.Column<int>(nullable: false),
                    PctA = table.Column<double>(nullable: false),
                    PctAMinus = table.Column<double>(nullable: false),
                    PctBPlus = table.Column<double>(nullable: false),
                    PctB = table.Column<double>(nullable: false),
                    PctBMinus = table.Column<double>(nullable: false),
                    PctCPlus = table.Column<double>(nullable: false),
                    PctC = table.Column<double>(nullable: false),
                    PctD = table.Column<double>(nullable: false),
                    PctF = table.Column<double>(nullable: false),
                    PctWithdraw = table.Column<double>(nullable: false),
                    MeanGpa = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IsqEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IsqEntries_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IsqEntries_Professors_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Professors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseCodes_CourseId",
                table: "CourseCodes",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseNames_CourseId",
                table: "CourseNames",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Description",
                table: "Courses",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_IsqEntries_CourseId",
                table: "IsqEntries",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_IsqEntries_ProfessorId",
                table: "IsqEntries",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_IsqEntries_Crn_Season_Year",
                table: "IsqEntries",
                columns: new[] { "Crn", "Season", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_Professors_NNumber",
                table: "Professors",
                column: "NNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseCodes");

            migrationBuilder.DropTable(
                name: "CourseNames");

            migrationBuilder.DropTable(
                name: "IsqEntries");

            migrationBuilder.DropTable(
                name: "Queries");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Professors");
        }
    }
}
