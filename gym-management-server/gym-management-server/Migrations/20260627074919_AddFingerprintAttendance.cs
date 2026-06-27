using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gym_management_server.Migrations
{
    /// <inheritdoc />
    public partial class AddFingerprintAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "CheckIns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                table: "CheckIns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperatorUserId",
                table: "CheckIns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Vendor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FingerprintTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FingerPosition = table.Column<byte>(type: "tinyint", nullable: false),
                    Template = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Vendor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quality = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FingerprintTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FingerprintTemplates_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_DeviceId",
                table: "CheckIns",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_MemberId_CheckOutTime",
                table: "CheckIns",
                columns: new[] { "MemberId", "CheckOutTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDevices_SerialNumber",
                table: "AttendanceDevices",
                column: "SerialNumber",
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintTemplates_MemberId",
                table: "FingerprintTemplates",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FingerprintTemplates_MemberId_FingerPosition",
                table: "FingerprintTemplates",
                columns: new[] { "MemberId", "FingerPosition" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckIns_AttendanceDevices_DeviceId",
                table: "CheckIns",
                column: "DeviceId",
                principalTable: "AttendanceDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckIns_AttendanceDevices_DeviceId",
                table: "CheckIns");

            migrationBuilder.DropTable(
                name: "AttendanceDevices");

            migrationBuilder.DropTable(
                name: "FingerprintTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_DeviceId",
                table: "CheckIns");

            migrationBuilder.DropIndex(
                name: "IX_CheckIns_MemberId_CheckOutTime",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "OperatorUserId",
                table: "CheckIns");
        }
    }
}
