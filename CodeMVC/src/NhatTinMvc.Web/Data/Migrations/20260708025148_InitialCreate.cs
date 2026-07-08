using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NhatTinMvc.Web.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackedBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RefCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    TotalFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastStatusId = table.Column<int>(type: "int", nullable: false),
                    LastStatusName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastStatusAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RawCreateResponse = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedBills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillStatusEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackedBillId = table.Column<int>(type: "int", nullable: true),
                    BillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    StatusName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusTime = table.Column<long>(type: "bigint", nullable: true),
                    PushTime = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DedupeKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillStatusEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillStatusEvents_TrackedBills_TrackedBillId",
                        column: x => x.TrackedBillId,
                        principalTable: "TrackedBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillStatusEvents_DedupeKey",
                table: "BillStatusEvents",
                column: "DedupeKey",
                unique: true,
                filter: "[DedupeKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillStatusEvents_TrackedBillId",
                table: "BillStatusEvents",
                column: "TrackedBillId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedBills_BillCode",
                table: "TrackedBills",
                column: "BillCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillStatusEvents");

            migrationBuilder.DropTable(
                name: "TrackedBills");
        }
    }
}
