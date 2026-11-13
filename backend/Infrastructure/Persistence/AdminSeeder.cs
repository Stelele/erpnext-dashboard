using Domain.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public static class AdminSeeder
{
    // Trusted-admin bootstrap from the Admins:Seed user secret. Runs on every
    // startup: creates missing users, promotes Viewers, leaves Admins alone.
    // The database is the source of truth for names, emails and passwords —
    // the seeder never overwrites an existing user's details, so a rename done
    // through the UI cannot be resurrected as a duplicate from stale config.
    public static async Task SeedAsync(DashboardDbContext db, IConfiguration config)
    {
        var seeds = config.GetSection("Admins:Seed").Get<List<SeedAdmin>>() ?? [];
        if (seeds.Count == 0)
            return;

        var changed = false;
        foreach (var seed in seeds)
        {
            var email = seed.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                db.Users.Add(User.Create(seed.Name, email, Role.Admin));
                changed = true;
            }
            else if (user.Role != Role.Admin)
            {
                user.Role = Role.Admin;
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync();
    }

    public sealed class SeedAdmin
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
