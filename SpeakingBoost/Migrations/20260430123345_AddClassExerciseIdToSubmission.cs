using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeakingBoost.Migrations
{
    /// <inheritdoc />
    public partial class AddClassExerciseIdToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassExerciseId",
                table: "Submissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ClassExerciseId",
                table: "Submissions",
                column: "ClassExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_ClassExercises_ClassExerciseId",
                table: "Submissions",
                column: "ClassExerciseId",
                principalTable: "ClassExercises",
                principalColumn: "ClassExerciseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_ClassExercises_ClassExerciseId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_ClassExerciseId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ClassExerciseId",
                table: "Submissions");
        }
    }
}
