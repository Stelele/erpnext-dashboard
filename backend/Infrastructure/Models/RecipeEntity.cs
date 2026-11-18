using Domain.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class RecipeEntity : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");

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
            new Recipe { Id = Guid.Parse("00000000-0000-0000-0002-000000000100"), Name = "Retail", Description = "Retail pages bundle", CreatedOn = seedDate, UpdatedOn = seedDate },
            new Recipe { Id = Guid.Parse("00000000-0000-0000-0002-000000000101"), Name = "Refund", Description = "Refund pages", CreatedOn = seedDate, UpdatedOn = seedDate },
            new Recipe { Id = Guid.Parse("00000000-0000-0000-0002-000000000102"), Name = "Service", Description = "Service pages bundle", CreatedOn = seedDate, UpdatedOn = seedDate }
        );
    }
}
