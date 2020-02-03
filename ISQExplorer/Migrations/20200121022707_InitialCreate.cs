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
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DepartmentId = table.Column<int>(nullable: false),
                    CourseCode = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Professors",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(nullable: false),
                    LastName = table.Column<string>(nullable: false),
                    NNumber = table.Column<string>(nullable: false),
                    DepartmentId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Professors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Professors_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IsqEntries",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(nullable: false),
                    Season = table.Column<int>(nullable: false),
                    Year = table.Column<int>(nullable: false),
                    ProfessorId = table.Column<int>(nullable: false),
                    Crn = table.Column<int>(nullable: false),
                    NResponded = table.Column<int>(nullable: false),
                    NEnrolled = table.Column<int>(nullable: false),
                    Pct5 = table.Column<double>(nullable: false),
                    Pct4 = table.Column<double>(nullable: false),
                    Pct3 = table.Column<double>(nullable: false),
                    Pct2 = table.Column<double>(nullable: false),
                    Pct1 = table.Column<double>(nullable: false),
                    PctNa = table.Column<double>(nullable: false),
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IsqEntries_Professors_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Professors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses",
                column: "CourseCode");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name");

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
                name: "IX_Professors_DepartmentId",
                table: "Professors",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Professors_NNumber",
                table: "Professors",
                column: "NNumber");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IsqEntries");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Professors");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
