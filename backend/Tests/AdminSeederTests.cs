using Domain.Users;
using Infrastructure.Models;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tests;

// Trusted-admin bootstrap: entries in the Admins:Seed user-secrets section are
// ensured as Admins on every startup. Config creates missing users and promotes
// existing Viewers; the database stays the source of truth for names, emails
// and passwords — the seeder never overwrites or re-adds them.
public class AdminSeederTests
{
    private static async Task<(DashboardDbContext Db, SqliteConnection Conn)> NewDbAsync()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        await conn.OpenAsync();
        var db = new DashboardDbContext(
            new DbContextOptionsBuilder<DashboardDbContext>().UseSqlite(conn).Options,
            new NoopPublisher());
        await db.Database.EnsureCreatedAsync();
        return (db, conn);
    }

    private static IConfiguration Config(params (string Name, string Email)[] admins)
    {
        var values = new Dictionary<string, string?>();
        for (var i = 0; i < admins.Length; i++)
        {
            values[$"Admins:Seed:{i}:Name"] = admins[i].Name;
            values[$"Admins:Seed:{i}:Email"] = admins[i].Email;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public async Task Missing_users_are_created_as_admins()
    {
        var (db, conn) = await NewDbAsync();
        await using var _ = conn;

        await AdminSeeder.SeedAsync(db, Config(
            ("Gift Mugweni", "gift@example.com"),
            ("Alice Moyo", "alice@example.com")));

        Assert.Equal(2, await db.Users.CountAsync());
        var gift = await db.Users.SingleAsync(u => u.Email == "gift@example.com");
        Assert.Equal("Gift Mugweni", gift.Name);
        Assert.Equal(Role.Admin, gift.Role);
        // no password: the first password is set through forgot-password → reset-password
        Assert.Null(gift.PasswordHash);
    }

    [Fact]
    public async Task Existing_viewers_are_promoted()
    {
        var (db, conn) = await NewDbAsync();
        await using var _ = conn;
        var viewer = User.Create("Gift Mugweni", "gift@example.com", Role.Viewer);
        viewer.PasswordHash = "existing-hash";
        db.Users.Add(viewer);
        await db.SaveChangesAsync();

        await AdminSeeder.SeedAsync(db, Config(("Gift Mugweni", "gift@example.com")));

        Assert.Equal(1, await db.Users.CountAsync());
        var promoted = await db.Users.SingleAsync(u => u.Email == "gift@example.com");
        Assert.Equal(Role.Admin, promoted.Role);
        // promotion must not touch the user's existing credentials or details
        Assert.Equal("existing-hash", promoted.PasswordHash);
        Assert.Equal("Gift Mugweni", promoted.Name);
    }

    [Fact]
    public async Task Existing_admins_are_left_alone()
    {
        var (db, conn) = await NewDbAsync();
        await using var _ = conn;
        var admin = User.Create("Gift M.", "gift@example.com", Role.Admin);
        admin.PasswordHash = "existing-hash";
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        await AdminSeeder.SeedAsync(db, Config(("Gift Mugweni", "gift@example.com")));

        Assert.Equal(1, await db.Users.CountAsync());
        var unchanged = await db.Users.SingleAsync();
        Assert.Equal(Role.Admin, unchanged.Role);
        // config name must not overwrite a rename done through the UI
        Assert.Equal("Gift M.", unchanged.Name);
    }

    [Fact]
    public async Task Emails_are_matched_after_trimming_and_lowercasing()
    {
        var (db, conn) = await NewDbAsync();
        await using var _ = conn;
        db.Users.Add(User.Create("Gift Mugweni", "gift@example.com", Role.Viewer));
        await db.SaveChangesAsync();

        await AdminSeeder.SeedAsync(db, Config(("Gift Mugweni", "  GIFT@Example.com ")));

        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(Role.Admin, (await db.Users.SingleAsync()).Role);
    }

    [Fact]
    public async Task Mixed_case_stored_email_is_matched_not_duplicated()
    {
        var (db, conn) = await NewDbAsync();
        await using var _ = conn;
        // emails are stored verbatim by CreateUserCommandHandler, so a stored
        // mixed-case email must still match the lowercased config entry
        db.Users.Add(User.Create("Gift Mugweni", "GIFT@Example.com", Role.Viewer));
        await db.SaveChangesAsync();

        await AdminSeeder.SeedAsync(db, Config(("Gift Mugweni", "gift@example.com")));

        Assert.Equal(1, await db.Users.CountAsync());
        var promoted = await db.Users.SingleAsync();
        Assert.Equal(Role.Admin, promoted.Role);
        Assert.Equal("GIFT@Example.com", promoted.Email);
    }

    [Fact]
    public async Task Entries_with_missing_name_or_email_are_skipped()
    {
        var (db, conn) = await NewDbAsync();
        await using var _ = conn;

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admins:Seed:0:Name"] = "No Email",
            ["Admins:Seed:1:Email"] = "gift@example.com",
            ["Admins:Seed:1:Name"] = "Gift Mugweni",
        }).Build();

        await AdminSeeder.SeedAsync(db, config);

        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal("Gift Mugweni", (await db.Users.SingleAsync()).Name);
    }

    [Fact]
    public async Task Missing_config_section_is_a_noop()
    {
        var (db, conn) = await NewDbAsync();
        await using var _ = conn;
        db.Users.Add(User.Create("Gift Mugweni", "gift@example.com", Role.Viewer));
        await db.SaveChangesAsync();

        await AdminSeeder.SeedAsync(db, new ConfigurationBuilder().Build());

        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(Role.Viewer, (await db.Users.SingleAsync()).Role);
    }
}

public class NoopPublisher : MediatR.IPublisher
{
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
