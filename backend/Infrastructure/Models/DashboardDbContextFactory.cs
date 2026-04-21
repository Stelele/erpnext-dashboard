using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Models;

public class DashboardDbContextFactory : IDesignTimeDbContextFactory<DashboardDbContext>
{
    public DashboardDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DashboardDbContext>();
        optionsBuilder.UseSqlite("Data Source=dashboard.db");
        return new DashboardDbContext(optionsBuilder.Options, NullPublisher.Instance);
    }
}

public class NullPublisher : IPublisher
{
    public static readonly NullPublisher Instance = new();

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;

    public Task Publish<TNotification>(IEnumerable<TNotification> notifications, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;
}