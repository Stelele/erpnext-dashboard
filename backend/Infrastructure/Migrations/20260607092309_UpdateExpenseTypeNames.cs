using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExpenseTypeNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000004",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Entertainment/Director's Expenses", "Entertainment/Owner" });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000005",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Marketing Expenses", "Marketing" });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000008",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Canteen", "Staff Canteen" });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000010",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Salary", "Staff Salary" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000004",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Entertainment expenses", "Entertainment" });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000005",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Maintenance costs", "Maintenance" });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000008",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Office supplies", "Office Supplies" });

            migrationBuilder.UpdateData(
                table: "ExpenseTypes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0001-000000000010",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Staff welfare expenses", "Staff Welfare" });
        }
    }
}
