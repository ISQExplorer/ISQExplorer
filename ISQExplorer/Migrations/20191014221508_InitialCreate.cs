using Microsoft.EntityFrameworkCore.Migrations;

namespace ISQExplorer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Professors",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    NNumber = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Professors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IsqEntries",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Term = table.Column<string>(nullable: true),
                    ProfessorId = table.Column<int>(nullable: true),
                    Crn = table.Column<int>(nullable: false),
                    NResponded = table.Column<int>(nullable: false),
                    NTotal = table.Column<int>(nullable: false),
                    Pct5 = table.Column<double>(nullable: false),
                    Pct4 = table.Column<double>(nullable: false),
                    Pct3 = table.Column<double>(nullable: false),
                    Pct2 = table.Column<double>(nullable: false),
                    Pct1 = table.Column<double>(nullable: false),
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
                        name: "FK_IsqEntries_Professors_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Professors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IsqEntries_ProfessorId",
                table: "IsqEntries",
                column: "ProfessorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IsqEntries");

            migrationBuilder.DropTable(
                name: "Professors");
        }
    }
}
