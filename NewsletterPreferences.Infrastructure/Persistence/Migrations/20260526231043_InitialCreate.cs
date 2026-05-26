using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NewsletterPreferences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunicationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterInterests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterInterests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriberTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriberTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Organisation = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SubscriberTypeId = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PostalAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConsentGiven = table.Column<bool>(type: "bit", nullable: false),
                    ConsentTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriberTypes_SubscriberTypeId",
                        column: x => x.SubscriberTypeId,
                        principalTable: "SubscriberTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionCommunicationPreferences",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommunicationPreferenceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionCommunicationPreferences", x => new { x.SubscriptionId, x.CommunicationPreferenceId });
                    table.ForeignKey(
                        name: "FK_SubscriptionCommunicationPreferences_CommunicationPreferences_CommunicationPreferenceId",
                        column: x => x.CommunicationPreferenceId,
                        principalTable: "CommunicationPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionCommunicationPreferences_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionInterests",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewsletterInterestId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionInterests", x => new { x.SubscriptionId, x.NewsletterInterestId });
                    table.ForeignKey(
                        name: "FK_SubscriptionInterests_NewsletterInterests_NewsletterInterestId",
                        column: x => x.NewsletterInterestId,
                        principalTable: "NewsletterInterests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionInterests_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CommunicationPreferences",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "EMAIL", "Email" },
                    { 2, "PHONE", "Phone" },
                    { 3, "SMS", "SMS" },
                    { 4, "POST", "Post" }
                });

            migrationBuilder.InsertData(
                table: "NewsletterInterests",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "HOUSES", "Houses" },
                    { 2, "APARTMENTS", "Apartments" },
                    { 3, "SHARED_OWNERSHIP", "Shared Ownership" },
                    { 4, "RENTAL", "Rental" },
                    { 5, "LAND_SOURCING", "Land Sourcing" },
                    { 6, "DEV_FINANCE", "Development Finance" },
                    { 7, "PLANNING_UPDATES", "Planning Updates" },
                    { 8, "NEW_DEVELOPMENTS", "New Developments" }
                });

            migrationBuilder.InsertData(
                table: "SubscriberTypes",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "HOME_BUYER", "Home Buyer" },
                    { 2, "HOME_BUILDER", "Home Builder" },
                    { 3, "LAND_AGENT", "Land Agent / Land Sourcer" },
                    { 4, "DEVELOPER", "Developer" },
                    { 5, "OTHER", "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationPreferences_Code",
                table: "CommunicationPreferences",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterInterests_Code",
                table: "NewsletterInterests",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriberTypes_Code",
                table: "SubscriberTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionCommunicationPreferences_CommunicationPreferenceId",
                table: "SubscriptionCommunicationPreferences",
                column: "CommunicationPreferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInterests_NewsletterInterestId",
                table: "SubscriptionInterests",
                column: "NewsletterInterestId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Email",
                table: "Subscriptions",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriberTypeId",
                table: "Subscriptions",
                column: "SubscriberTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionCommunicationPreferences");

            migrationBuilder.DropTable(
                name: "SubscriptionInterests");

            migrationBuilder.DropTable(
                name: "CommunicationPreferences");

            migrationBuilder.DropTable(
                name: "NewsletterInterests");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "SubscriberTypes");
        }
    }
}
