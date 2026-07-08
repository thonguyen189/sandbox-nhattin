using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NhatTinWebhookReceiver.Api.Persistence.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivedWebhooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsValidPayload = table.Column<bool>(type: "bit", nullable: false),
                    BillNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    StatusName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusTime = table.Column<long>(type: "bigint", nullable: true),
                    PushTime = table.Column<long>(type: "bigint", nullable: true),
                    DedupeKey = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedWebhooks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedWebhooks_DedupeKey",
                table: "ReceivedWebhooks",
                column: "DedupeKey",
                unique: true,
                filter: "[DedupeKey] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivedWebhooks");
        }
    }
}
