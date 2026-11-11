using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamService.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeExecutionResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutionError",
                table: "Answers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionStatus",
                table: "Answers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PassedTests",
                table: "Answers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "Answers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTests",
                table: "Answers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutionError",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "ExecutionStatus",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "PassedTests",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "TotalTests",
                table: "Answers");
        }
    }
}
