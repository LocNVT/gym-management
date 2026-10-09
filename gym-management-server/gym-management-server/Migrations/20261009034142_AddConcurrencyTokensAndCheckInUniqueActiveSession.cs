using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gym_management_server.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyTokensAndCheckInUniqueActiveSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Trainers",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServicePackages",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Members",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InvoiceItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Expenses",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CheckIns",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            // Pre-existing data may already have more than one open session for the same member
            // (exactly the bug docs/ImprovementPlan.md mục 4 describes: nothing ever auto-closed a
            // forgotten check-in). The unique index below would fail to create on such data, so
            // close every open session except the most recently started one per member first.
            migrationBuilder.Sql(@"
                ;WITH RankedActiveSessions AS (
                    SELECT Id, CheckInTime,
                           ROW_NUMBER() OVER (PARTITION BY MemberId ORDER BY CheckInTime DESC, Id DESC) AS RowNum
                    FROM CheckIns
                    WHERE CheckOutTime IS NULL
                )
                UPDATE c
                SET c.CheckOutTime = c.CheckInTime
                FROM CheckIns c
                JOIN RankedActiveSessions r ON c.Id = r.Id
                WHERE r.RowNum > 1;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_MemberId_ActiveSession",
                table: "CheckIns",
                column: "MemberId",
                unique: true,
                filter: "[CheckOutTime] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CheckIns_MemberId_ActiveSession",
                table: "CheckIns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServicePackages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CheckIns");
        }
    }
}
