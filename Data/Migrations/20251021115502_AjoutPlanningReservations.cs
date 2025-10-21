using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CTSAR.Booking.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPlanningReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alveoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    EstActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alveoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateDebut = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StatutReservation = table.Column<int>(type: "INTEGER", nullable: false),
                    Commentaire = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FermetureAlveoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlveoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Raison = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TypeFermeture = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ReservationAlveoles",
                columns: table => new
                {
                    ReservationId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlveoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationAlveoles", x => new { x.ReservationId, x.AlveoleId });
                    table.ForeignKey(
                        name: "FK_ReservationAlveoles_Alveoles_AlveoleId",
                        column: x => x.AlveoleId,
                        principalTable: "Alveoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationAlveoles_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservationParticipants",
                columns: table => new
                {
                    ReservationId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DateInscription = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstMoniteur = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationParticipants", x => new { x.ReservationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ReservationParticipants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationParticipants_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alveoles_Nom",
                table: "Alveoles",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alveoles_Ordre",
                table: "Alveoles",
                column: "Ordre");

            migrationBuilder.CreateIndex(
                name: "IX_FermetureAlveoles_AlveoleId",
                table: "FermetureAlveoles",
                column: "AlveoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAlveoles_AlveoleId",
                table: "ReservationAlveoles",
                column: "AlveoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationParticipants_UserId",
                table: "ReservationParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CreatedByUserId",
                table: "Reservations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DateDebut",
                table: "Reservations",
                column: "DateDebut");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DateFin",
                table: "Reservations",
                column: "DateFin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FermetureAlveoles");

            migrationBuilder.DropTable(
                name: "ReservationAlveoles");

            migrationBuilder.DropTable(
                name: "ReservationParticipants");

            migrationBuilder.DropTable(
                name: "Alveoles");

            migrationBuilder.DropTable(
                name: "Reservations");
        }
    }
}
