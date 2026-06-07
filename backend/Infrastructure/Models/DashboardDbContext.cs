using Domain.Abstractions;
using Domain.Companies;
using Domain.CompanyExpenseMappings;
using Domain.CompanySettings;
using Domain.ExpenseTypes;
using Domain.Sites;
using Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Models;

public class DashboardDbContext(DbContextOptions<DashboardDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<Site> Sites { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<ExpenseType> ExpenseTypes { get; set; }
    public DbSet<CompanyExpenseMapping> CompanyExpenseMappings { get; set; }
    public DbSet<CompanySettings> CompanySettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new SiteEntity().Configure(modelBuilder.Entity<Site>());
        new UserEntity().Configure(modelBuilder.Entity<User>());
        new CompanyEntity().Configure(modelBuilder.Entity<Company>());
        new ExpenseTypeEntity().Configure(modelBuilder.Entity<ExpenseType>());
        new CompanyExpenseMappingEntity().Configure(modelBuilder.Entity<CompanyExpenseMapping>());
        new CompanySettingsEntity().Configure(modelBuilder.Entity<CompanySettings>());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker.Entries<Base>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var _event in events)
            await publisher.Publish(_event, cancellationToken);

        foreach (var entity in entities)
            entity.ClearDomainEvents();

        return result;
    }
}
