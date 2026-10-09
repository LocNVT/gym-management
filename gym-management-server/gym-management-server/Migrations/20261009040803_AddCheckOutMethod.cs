using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gym_management_server.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckOutMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "CheckOutMethod",
                table: "CheckIns",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckOutMethod",
                table: "CheckIns");
        }
    }
}
