using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionOrderIdToBulkImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubscriptionOrderId",
                table: "BulkImportJobs",
                type: "integer",
                nullable: true,
                comment: "Optional SubscriptionOrderId preferred for this bulk job");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionOrderId",
                table: "BulkImportJobs");
        }
    }
}
