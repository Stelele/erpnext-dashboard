using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseTypeSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ExpenseTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000001",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000002",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000003",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000004",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000005",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000006",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000007",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000008",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000009",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000010",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000011",
                column: "IsDeleted",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ExpenseTypes");
        }
    }
}
