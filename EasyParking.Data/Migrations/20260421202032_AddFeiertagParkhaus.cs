using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyParking.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeiertagParkhaus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParkhausId",
                table: "Feiertage",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feiertage_Datum_ParkhausId",
                table: "Feiertage",
                columns: new[] { "Datum", "ParkhausId" });

            migrationBuilder.CreateIndex(
                name: "IX_Feiertage_ParkhausId",
                table: "Feiertage",
                column: "ParkhausId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feiertage_Parkhaeuser_ParkhausId",
                table: "Feiertage",
                column: "ParkhausId",
                principalTable: "Parkhaeuser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feiertage_Parkhaeuser_ParkhausId",
                table: "Feiertage");

            migrationBuilder.DropIndex(
                name: "IX_Feiertage_Datum_ParkhausId",
                table: "Feiertage");

            migrationBuilder.DropIndex(
                name: "IX_Feiertage_ParkhausId",
                table: "Feiertage");

            migrationBuilder.DropColumn(
                name: "ParkhausId",
                table: "Feiertage");
        }
    }
}
