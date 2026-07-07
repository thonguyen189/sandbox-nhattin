using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NhatTinWebhookReceiver.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookDedupe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DedupeKey",
                table: "ReceivedWebhooks",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PushTime",
                table: "ReceivedWebhooks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StatusTime",
                table: "ReceivedWebhooks",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedWebhooks_DedupeKey",
                table: "ReceivedWebhooks",
                column: "DedupeKey",
                unique: true,
                filter: "[DedupeKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReceivedWebhooks_DedupeKey",
                table: "ReceivedWebhooks");

            migrationBuilder.DropColumn(
                name: "DedupeKey",
                table: "ReceivedWebhooks");

            migrationBuilder.DropColumn(
                name: "PushTime",
                table: "ReceivedWebhooks");

            migrationBuilder.DropColumn(
                name: "StatusTime",
                table: "ReceivedWebhooks");
        }
    }
}
