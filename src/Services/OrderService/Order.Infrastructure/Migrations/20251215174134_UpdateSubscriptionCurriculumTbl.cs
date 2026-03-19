using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Order.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionCurriculumTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "CourseIds",
                table: "SubscriptionOrderCurriculums",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "EmulatorModelIds",
                table: "SubscriptionOrderCurriculums",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseIds",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.DropColumn(
                name: "EmulatorModelIds",
                table: "SubscriptionOrderCurriculums");
        }
    }
}
