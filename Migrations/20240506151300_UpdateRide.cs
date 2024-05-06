using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drive_Mate_Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RideDate",
                table: "Rides",
                newName: "StartDate");

            migrationBuilder.AddColumn<string>(
                name: "Car",
                table: "Rides",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Rides",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Car",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Rides");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Rides",
                newName: "RideDate");
        }
    }
}
