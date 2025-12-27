using Domain.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class SiteEntity : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder
            .HasKey(b => b.Id);

        builder
            .Property(b => b.Name)
            .IsRequired();

        builder
            .Property(b => b.Description)
            .IsRequired(false);

        builder
            .Property(b => b.Url)
            .IsRequired();

        builder
            .HasIndex(b => b.Url)
            .IsUnique();

        builder
            .Property(b => b.ApiToken)
            .IsRequired();


        builder
            .HasMany(b => b.Users)
            .WithMany(b => b.Sites);

        builder
            .HasMany(b => b.Companies)
            .WithOne(b => b.Site);

        builder
            .Property(b => b.CreatedOn)
            .IsRequired();

        builder
            .Property(b => b.UpdatedOn)
            .IsRequired();
    }
}
