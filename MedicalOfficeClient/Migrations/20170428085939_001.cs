using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MedicalOfficeClient.Migrations
{
    public partial class _001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(nullable: false),
                    Birthday = table.Column<DateTime>(nullable: false),
                    DateChanged = table.Column<DateTime>(nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: true),
                    FirstName = table.Column<string>(nullable: false),
                    LastName = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    UserChanged = table.Column<string>(nullable: true),
                    UserCreated = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.PersonId);
                });

            migrationBuilder.CreateTable(
                name: "MedicalCases",
                columns: table => new
                {
                    MedicalCaseId = table.Column<Guid>(nullable: false),
                    DateChanged = table.Column<DateTime>(nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: true),
                    Label = table.Column<string>(nullable: true),
                    PersonId = table.Column<Guid>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    UserChanged = table.Column<string>(nullable: true),
                    UserCreated = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCases", x => x.MedicalCaseId);
                    table.ForeignKey(
                        name: "FK_MedicalCases_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalItems",
                columns: table => new
                {
                    MedicalItemId = table.Column<Guid>(nullable: false),
                    Content = table.Column<byte[]>(nullable: true),
                    DateChanged = table.Column<DateTime>(nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: true),
                    Element = table.Column<byte[]>(nullable: true),
                    Label = table.Column<string>(nullable: true),
                    MedicalCaseId = table.Column<Guid>(nullable: false),
                    Overlay = table.Column<byte[]>(nullable: true),
                    Preview = table.Column<byte[]>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    UserChanged = table.Column<string>(nullable: true),
                    UserCreated = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalItems", x => x.MedicalItemId);
                    table.ForeignKey(
                        name: "FK_MedicalItems_MedicalCases_MedicalCaseId",
                        column: x => x.MedicalCaseId,
                        principalTable: "MedicalCases",
                        principalColumn: "MedicalCaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCases_PersonId",
                table: "MedicalCases",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalItems_MedicalCaseId",
                table: "MedicalItems",
                column: "MedicalCaseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalItems");

            migrationBuilder.DropTable(
                name: "MedicalCases");

            migrationBuilder.DropTable(
                name: "Persons");
        }
    }
}
