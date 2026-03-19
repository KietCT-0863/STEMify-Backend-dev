using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resource.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAssignmentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Sections_SectionId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignment_SectionId",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "SectionId",
                table: "Assignments",
                newName: "ContentId");

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "Contents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Text",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_SectionId",
                table: "Assignments",
                column: "ContentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Contents_ContentId",
                table: "Assignments",
                column: "ContentId",
                principalTable: "Contents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Contents_ContentId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignment_SectionId",
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "ContentId",
                table: "Assignments",
                newName: "SectionId");

            migrationBuilder.AlterColumn<int>(
                name: "ContentType",
                table: "Contents",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Text");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_SectionId",
                table: "Assignments",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Sections_SectionId",
                table: "Assignments",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
