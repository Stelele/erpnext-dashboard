using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Models;

public class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<Site> Sites { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new SiteEntity().Configure(modelBuilder.Entity<Site>());
        new UserEntity().Configure(modelBuilder.Entity<User>());
        new CompanyEntity().Configure(modelBuilder.Entity<Company>());
    }
}
