using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resource.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddName_AndPK_LessonAssetTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "LessonAssets",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LessonAssets_LessonId",
                table: "LessonAssets",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonAssets_Lesson",
                table: "LessonAssets",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonAssets_Lesson",
                table: "LessonAssets");

            migrationBuilder.DropIndex(
                name: "IX_LessonAssets_LessonId",
                table: "LessonAssets");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "LessonAssets");
        }
    }
}
