using System.Text.Json;
using Domain.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public static class AdminSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Trusted-admin bootstrap from the Admins:Seed user secret. Runs on every
    // startup: creates missing users, promotes Viewers, leaves Admins alone.
    // The database is the source of truth for names, emails and passwords —
    // the seeder never overwrites an existing user's details, so a rename done
    // through the UI cannot be resurrected as a duplicate from stale config.
    // The value is a JSON array string ([{"Name":..,"Email":..}]) so it reads
    // identically from user secrets (Admins:Seed) and env vars (Admins__Seed).
    public static async Task SeedAsync(DashboardDbContext db, IConfiguration config, CancellationToken ct = default)
    {
        var json = config["Admins:Seed"];
        if (string.IsNullOrWhiteSpace(json))
            return;

        var seeds = JsonSerializer.Deserialize<List<SeedAdmin>>(json, JsonOptions) ?? [];
        if (seeds.Count == 0)
            return;

        var changed = false;
        foreach (var seed in seeds)
        {
            if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Name))
                continue;

            var email = seed.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
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
            await db.SaveChangesAsync(ct);
    }

    public sealed class SeedAdmin
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
