using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Order.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionCurriculumV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseIds",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.DropColumn(
                name: "EmulatorModelIds",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.AddColumn<string>(
                name: "CoursesSnapshot",
                table: "SubscriptionOrderCurriculums",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurriculumCode",
                table: "SubscriptionOrderCurriculums",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurriculumDescription",
                table: "SubscriptionOrderCurriculums",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurriculumImageUrl",
                table: "SubscriptionOrderCurriculums",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurriculumTitle",
                table: "SubscriptionOrderCurriculums",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmulatorsSnapshot",
                table: "SubscriptionOrderCurriculums",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoursesSnapshot",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.DropColumn(
                name: "CurriculumCode",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.DropColumn(
                name: "CurriculumDescription",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.DropColumn(
                name: "CurriculumImageUrl",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.DropColumn(
                name: "CurriculumTitle",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.DropColumn(
                name: "EmulatorsSnapshot",
                table: "SubscriptionOrderCurriculums");

            migrationBuilder.AddColumn<int[]>(
                name: "CourseIds",
                table: "SubscriptionOrderCurriculums",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "EmulatorModelIds",
                table: "SubscriptionOrderCurriculums",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }
    }
}
