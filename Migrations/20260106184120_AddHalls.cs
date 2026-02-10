using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaSystem.Desktop.Migrations
{
    /// <inheritdoc />
    public partial class AddHalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_Films_FilmId",
                table: "Screenings");

            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_Halls_HallId",
                table: "Screenings");

            migrationBuilder.DropIndex(
                name: "IX_Screenings_FilmId",
                table: "Screenings");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Screenings");

            migrationBuilder.DropColumn(
                name: "FilmId",
                table: "Screenings");

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Screenings",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "MovieId",
                table: "Screenings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedAt",
                table: "Films",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Screenings_MovieId",
                table: "Screenings",
                column: "MovieId");

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_Films_MovieId",
                table: "Screenings",
                column: "MovieId",
                principalTable: "Films",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_Halls_HallId",
                table: "Screenings",
                column: "HallId",
                principalTable: "Halls",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_Films_MovieId",
                table: "Screenings");

            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_Halls_HallId",
                table: "Screenings");

            migrationBuilder.DropIndex(
                name: "IX_Screenings_MovieId",
                table: "Screenings");

            migrationBuilder.DropColumn(
                name: "MovieId",
                table: "Screenings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Films");

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Screenings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Screenings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FilmId",
                table: "Screenings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Screenings_FilmId",
                table: "Screenings",
                column: "FilmId");

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_Films_FilmId",
                table: "Screenings",
                column: "FilmId",
                principalTable: "Films",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_Halls_HallId",
                table: "Screenings",
                column: "HallId",
                principalTable: "Halls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
