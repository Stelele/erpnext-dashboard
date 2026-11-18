using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRefundAndRenameExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000003",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[] { "expense", "Expense pages", "Expense" });

            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "Id", "Code", "CreatedOn", "Description", "IsDeleted", "Name", "UpdatedOn" },
                values: new object[] { "00000000-0000-0000-0002-000000000004", "refund", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Refund pages", false, "Refund", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000100",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Retail pages bundle", "Retail" });

            migrationBuilder.InsertData(
                table: "Recipes",
                columns: new[] { "Id", "CreatedOn", "Description", "IsDeleted", "Name", "UpdatedOn" },
                values: new object[,]
                {
                    { "00000000-0000-0000-0002-000000000101", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Refund pages", false, "Refund", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "00000000-0000-0000-0002-000000000102", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Service pages bundle", false, "Service", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "RecipeFeatures",
                columns: new[] { "Id", "CreatedOn", "FeatureId", "RecipeId", "UpdatedOn" },
                values: new object[,]
                {
                    { "00000000-0000-0000-0002-000000000203", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0002-000000000004", "00000000-0000-0000-0002-000000000101", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "00000000-0000-0000-0002-000000000204", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0002-000000000001", "00000000-0000-0000-0002-000000000102", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "00000000-0000-0000-0002-000000000205", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "00000000-0000-0000-0002-000000000003", "00000000-0000-0000-0002-000000000102", new DateTimeOffset(new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RecipeFeatures",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000203");

            migrationBuilder.DeleteData(
                table: "RecipeFeatures",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000204");

            migrationBuilder.DeleteData(
                table: "RecipeFeatures",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000205");

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000004");

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000101");

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000102");

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000003",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[] { "expenses", "Expenses pages", "Expenses" });

            migrationBuilder.UpdateData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0002-000000000100",
                columns: new[] { "Description", "Name" },
                values: new object[] { "Default recipe", "Default" });
        }
    }
}
