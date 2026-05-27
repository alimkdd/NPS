using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsletterPreferences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPagedSortIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CreatedAt_Live",
                table: "Subscriptions",
                column: "CreatedAt",
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CreatedAt_Live",
                table: "Subscriptions");
        }
    }
}
