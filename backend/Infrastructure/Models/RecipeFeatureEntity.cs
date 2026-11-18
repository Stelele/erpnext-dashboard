using Domain.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class RecipeFeatureEntity : IEntityTypeConfiguration<RecipeFeature>
{
    public void Configure(EntityTypeBuilder<RecipeFeature> builder)
    {
        builder.ToTable("RecipeFeatures");

        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(e => e.RecipeId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(e => e.FeatureId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .HasIndex(e => new { e.RecipeId, e.FeatureId })
            .IsUnique();

        builder
            .HasOne(e => e.Recipe)
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Feature)
            .WithMany()
            .HasForeignKey(e => e.FeatureId)
            .OnDelete(DeleteBehavior.Restrict);

        var seedDate = new DateTimeOffset(2026, 6, 6, 0, 0, 0, TimeSpan.Zero);
        var retailRecipeId = Guid.Parse("00000000-0000-0000-0002-000000000100");
        var refundRecipeId = Guid.Parse("00000000-0000-0000-0002-000000000101");
        var serviceRecipeId = Guid.Parse("00000000-0000-0000-0002-000000000102");

        builder.HasData(
            new RecipeFeature { Id = Guid.Parse("00000000-0000-0000-0002-000000000200"), RecipeId = retailRecipeId, FeatureId = Guid.Parse("00000000-0000-0000-0002-000000000001"), CreatedOn = seedDate, UpdatedOn = seedDate },
            new RecipeFeature { Id = Guid.Parse("00000000-0000-0000-0002-000000000201"), RecipeId = retailRecipeId, FeatureId = Guid.Parse("00000000-0000-0000-0002-000000000002"), CreatedOn = seedDate, UpdatedOn = seedDate },
            new RecipeFeature { Id = Guid.Parse("00000000-0000-0000-0002-000000000202"), RecipeId = retailRecipeId, FeatureId = Guid.Parse("00000000-0000-0000-0002-000000000003"), CreatedOn = seedDate, UpdatedOn = seedDate },
            new RecipeFeature { Id = Guid.Parse("00000000-0000-0000-0002-000000000203"), RecipeId = refundRecipeId, FeatureId = Guid.Parse("00000000-0000-0000-0002-000000000004"), CreatedOn = seedDate, UpdatedOn = seedDate },
            new RecipeFeature { Id = Guid.Parse("00000000-0000-0000-0002-000000000204"), RecipeId = serviceRecipeId, FeatureId = Guid.Parse("00000000-0000-0000-0002-000000000001"), CreatedOn = seedDate, UpdatedOn = seedDate },
            new RecipeFeature { Id = Guid.Parse("00000000-0000-0000-0002-000000000205"), RecipeId = serviceRecipeId, FeatureId = Guid.Parse("00000000-0000-0000-0002-000000000003"), CreatedOn = seedDate, UpdatedOn = seedDate }
        );
    }
}
