﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Drive_Mate_Server.Migrations
{
    /// <inheritdoc />
    public partial class StatusOnRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Rides",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Rides");
        }
    }
}
