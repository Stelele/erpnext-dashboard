using Domain.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class FeatureEntity : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("Features");

        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder
            .Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder
            .Property(e => e.Description)
            .HasMaxLength(255);

        builder
            .HasIndex(e => e.Code)
            .IsUnique();

        builder.HasQueryFilter(e => !e.IsDeleted);

        var seedDate = new DateTimeOffset(2026, 6, 6, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            new Feature { Id = Guid.Parse("00000000-0000-0000-0002-000000000001"), Code = "sales", Name = "Sales", Description = "Sales pages", CreatedOn = seedDate, UpdatedOn = seedDate },
            new Feature { Id = Guid.Parse("00000000-0000-0000-0002-000000000002"), Code = "stock", Name = "Stock", Description = "Stock pages", CreatedOn = seedDate, UpdatedOn = seedDate },
            new Feature { Id = Guid.Parse("00000000-0000-0000-0002-000000000003"), Code = "expenses", Name = "Expenses", Description = "Expenses pages", CreatedOn = seedDate, UpdatedOn = seedDate }
        );
    }
}
