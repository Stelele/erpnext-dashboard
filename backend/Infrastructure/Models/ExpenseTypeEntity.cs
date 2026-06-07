using Domain.ExpenseTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class ExpenseTypeEntity : IEntityTypeConfiguration<ExpenseType>
{
    public void Configure(EntityTypeBuilder<ExpenseType> builder)
    {
        builder.ToTable("ExpenseTypes");

        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder
            .Property(e => e.Description)
            .HasMaxLength(255);

        builder
            .HasIndex(e => e.Name)
            .IsUnique();

        builder.HasQueryFilter(e => !e.IsDeleted);

        var seedDate = new DateTimeOffset(2026, 6, 6, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000001"), Name = "Utilities", Description = "Utility expenses", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000002"), Name = "Consumables", Description = "Consumable supplies", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000003"), Name = "Administrative", Description = "Administrative expenses", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000004"), Name = "Entertainment/Owner", Description = "Entertainment/Director's Expenses", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000005"), Name = "Marketing", Description = "Marketing Expenses", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000006"), Name = "Rent", Description = "Rent payments", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000007"), Name = "Travel", Description = "Travel expenses", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000008"), Name = "Staff Canteen", Description = "Canteen", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000009"), Name = "Professional Fees", Description = "Professional service fees", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000010"), Name = "Staff Salary", Description = "Salary", CreatedOn = seedDate, UpdatedOn = seedDate },
            new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000011"), Name = "Other", Description = "Other expenses", CreatedOn = seedDate, UpdatedOn = seedDate }
        );
    }
}
