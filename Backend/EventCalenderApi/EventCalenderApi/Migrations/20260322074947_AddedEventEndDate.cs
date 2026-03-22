using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCalenderApi.Migrations
{
    /// <inheritdoc />
    public partial class AddedEventEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EventEndDate",
                table: "Events",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventEndDate",
                table: "Events");
        }
    }
}
