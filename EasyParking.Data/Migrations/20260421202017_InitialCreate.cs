using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyParking.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feiertage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Bezeichnung = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feiertage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parkhaeuser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Stadt = table.Column<string>(type: "TEXT", nullable: false),
                    Gruppe = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parkhaeuser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stockwerke",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParkhausId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                    Bezeichnung = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stockwerke", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stockwerke_Parkhaeuser_ParkhausId",
                        column: x => x.ParkhausId,
                        principalTable: "Parkhaeuser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tarife",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParkhausId = table.Column<int>(type: "INTEGER", nullable: false),
                    Typ = table.Column<int>(type: "INTEGER", nullable: false),
                    StartStunde = table.Column<int>(type: "INTEGER", nullable: false),
                    EndStunde = table.Column<int>(type: "INTEGER", nullable: false),
                    PreisProStunde = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tarife", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tarife_Parkhaeuser_ParkhausId",
                        column: x => x.ParkhausId,
                        principalTable: "Parkhaeuser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parkplaetze",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StockwerkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                    Typ = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DauermieterId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parkplaetze", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parkplaetze_Stockwerke_StockwerkId",
                        column: x => x.StockwerkId,
                        principalTable: "Stockwerke",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dauermieter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParkhausId = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Vorname = table.Column<string>(type: "TEXT", nullable: false),
                    Nachname = table.Column<string>(type: "TEXT", nullable: false),
                    FesterParkplatzId = table.Column<int>(type: "INTEGER", nullable: true),
                    Gesperrt = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dauermieter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dauermieter_Parkhaeuser_ParkhausId",
                        column: x => x.ParkhausId,
                        principalTable: "Parkhaeuser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Dauermieter_Parkplaetze_FesterParkplatzId",
                        column: x => x.FesterParkplatzId,
                        principalTable: "Parkplaetze",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Mietzahlungen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DauermieterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Jahr = table.Column<int>(type: "INTEGER", nullable: false),
                    Monat = table.Column<int>(type: "INTEGER", nullable: false),
                    Betrag = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Zahldatum = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mietzahlungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mietzahlungen_Dauermieter_DauermieterId",
                        column: x => x.DauermieterId,
                        principalTable: "Dauermieter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parktickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketNummer = table.Column<string>(type: "TEXT", nullable: false),
                    ParkhausId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParkplatzId = table.Column<int>(type: "INTEGER", nullable: false),
                    DauermieterId = table.Column<int>(type: "INTEGER", nullable: true),
                    Kategorie = table.Column<int>(type: "INTEGER", nullable: false),
                    EingangsZeit = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AusgangsZeit = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Betrag = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Bezahlt = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parktickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parktickets_Dauermieter_DauermieterId",
                        column: x => x.DauermieterId,
                        principalTable: "Dauermieter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Parktickets_Parkhaeuser_ParkhausId",
                        column: x => x.ParkhausId,
                        principalTable: "Parkhaeuser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parktickets_Parkplaetze_ParkplatzId",
                        column: x => x.ParkplatzId,
                        principalTable: "Parkplaetze",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dauermieter_Code",
                table: "Dauermieter",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dauermieter_FesterParkplatzId",
                table: "Dauermieter",
                column: "FesterParkplatzId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dauermieter_ParkhausId",
                table: "Dauermieter",
                column: "ParkhausId");

            migrationBuilder.CreateIndex(
                name: "IX_Mietzahlungen_DauermieterId",
                table: "Mietzahlungen",
                column: "DauermieterId");

            migrationBuilder.CreateIndex(
                name: "IX_Parkplaetze_StockwerkId",
                table: "Parkplaetze",
                column: "StockwerkId");

            migrationBuilder.CreateIndex(
                name: "IX_Parktickets_DauermieterId",
                table: "Parktickets",
                column: "DauermieterId");

            migrationBuilder.CreateIndex(
                name: "IX_Parktickets_ParkhausId",
                table: "Parktickets",
                column: "ParkhausId");

            migrationBuilder.CreateIndex(
                name: "IX_Parktickets_ParkplatzId",
                table: "Parktickets",
                column: "ParkplatzId");

            migrationBuilder.CreateIndex(
                name: "IX_Parktickets_TicketNummer",
                table: "Parktickets",
                column: "TicketNummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stockwerke_ParkhausId",
                table: "Stockwerke",
                column: "ParkhausId");

            migrationBuilder.CreateIndex(
                name: "IX_Tarife_ParkhausId",
                table: "Tarife",
                column: "ParkhausId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feiertage");

            migrationBuilder.DropTable(
                name: "Mietzahlungen");

            migrationBuilder.DropTable(
                name: "Parktickets");

            migrationBuilder.DropTable(
                name: "Tarife");

            migrationBuilder.DropTable(
                name: "Dauermieter");

            migrationBuilder.DropTable(
                name: "Parkplaetze");

            migrationBuilder.DropTable(
                name: "Stockwerke");

            migrationBuilder.DropTable(
                name: "Parkhaeuser");
        }
    }
}
