using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NhatTinSandbox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BillCode = table.Column<string>(type: "TEXT", nullable: false),
                    RefCode = table.Column<string>(type: "TEXT", nullable: true),
                    PackageNo = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    Width = table.Column<double>(type: "REAL", nullable: false),
                    Length = table.Column<double>(type: "REAL", nullable: false),
                    Height = table.Column<double>(type: "REAL", nullable: false),
                    CargoContent = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsReturnDoc = table.Column<int>(type: "INTEGER", nullable: false),
                    CodAmount = table.Column<double>(type: "REAL", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CargoValue = table.Column<double>(type: "REAL", nullable: false),
                    CargoTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    SenderName = table.Column<string>(type: "TEXT", nullable: false),
                    SenderPhone = table.Column<string>(type: "TEXT", nullable: false),
                    SenderAddress = table.Column<string>(type: "TEXT", nullable: false),
                    SenderProvinceCode = table.Column<string>(type: "TEXT", nullable: false),
                    SenderWardCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsReturnOrg = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnName = table.Column<string>(type: "TEXT", nullable: true),
                    ReturnPhone = table.Column<string>(type: "TEXT", nullable: true),
                    ReturnAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ReturnProvinceCode = table.Column<string>(type: "TEXT", nullable: true),
                    ReturnWardCode = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiverName = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiverPhone = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiverAddress = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiverProvinceCode = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiverWardCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsDraft = table.Column<int>(type: "INTEGER", nullable: false),
                    OtherFee = table.Column<double>(type: "REAL", nullable: false),
                    IsInstallation = table.Column<int>(type: "INTEGER", nullable: false),
                    BillType = table.Column<int>(type: "INTEGER", nullable: false),
                    BillReturn = table.Column<string>(type: "TEXT", nullable: true),
                    StatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    MainFee = table.Column<double>(type: "REAL", nullable: false),
                    TotalFee = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ParentCode = table.Column<string>(type: "TEXT", nullable: true),
                    DistrictCode = table.Column<string>(type: "TEXT", nullable: true),
                    IsNew = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StatusCode = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BillId = table.Column<int>(type: "INTEGER", nullable: false),
                    BillCode = table.Column<string>(type: "TEXT", nullable: false),
                    SubscriptionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CallbackUrl = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResponseBody = table.Column<string>(type: "TEXT", nullable: true),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CallbackUrl = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BillId = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusName = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    ShippingFee = table.Column<double>(type: "REAL", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillStatusHistories_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BillCode",
                table: "Bills",
                column: "BillCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillStatusHistories_BillId",
                table: "BillStatusHistories",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Kind_Code",
                table: "Locations",
                columns: new[] { "Kind", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_MasterData_Kind_Code",
                table: "MasterData",
                columns: new[] { "Kind", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveryLogs_BillCode",
                table: "WebhookDeliveryLogs",
                column: "BillCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillStatusHistories");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "MasterData");

            migrationBuilder.DropTable(
                name: "PartnerAccounts");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "WebhookDeliveryLogs");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions");

            migrationBuilder.DropTable(
                name: "Bills");
        }
    }
}
