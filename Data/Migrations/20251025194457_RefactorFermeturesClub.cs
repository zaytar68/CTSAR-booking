using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTSAR.Booking.Migrations
{
    /// <inheritdoc />
    public partial class RefactorFermeturesClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FermetureAlveoles");

            migrationBuilder.CreateTable(
                name: "FermeturesClub",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateDebut = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Raison = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TypeFermeture = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FermeturesClub", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FermeturesClub_DateDebut",
                table: "FermeturesClub",
                column: "DateDebut");

            migrationBuilder.CreateIndex(
                name: "IX_FermeturesClub_DateFin",
                table: "FermeturesClub",
                column: "DateFin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FermeturesClub");

            migrationBuilder.CreateTable(
                name: "FermetureAlveoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlveoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Raison = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TypeFermeture = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FermetureAlveoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FermetureAlveoles_Alveoles_AlveoleId",
                        column: x => x.AlveoleId,
                        principalTable: "Alveoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FermetureAlveoles_AlveoleId",
                table: "FermetureAlveoles",
                column: "AlveoleId");
        }
    }
}
