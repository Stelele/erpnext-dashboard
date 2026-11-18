using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultRecipeAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-time rollout seeding: every company present at migration
            // time gets the seeded "Default" recipe (feature gating is new,
            // so no strips or custom sets exist at rollout). INSERT OR IGNORE
            // makes this idempotent against databases that already hold
            // runtime-seeded rows.
            migrationBuilder.Sql("""
                INSERT OR IGNORE INTO "CompanyRecipes" ("Id","CompanyId","RecipeId","CreatedOn","UpdatedOn")
                SELECT lower(hex(randomblob(16))), c."Id", '00000000-0000-0000-0002-000000000100', '2026-11-18T00:00:00+00:00', '2026-11-18T00:00:00+00:00' FROM "Companies" c;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "CompanyRecipes" WHERE "RecipeId" = '00000000-0000-0000-0002-000000000100';
                """);
        }
    }
}
