using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class CompanyEntity : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder
            .HasKey(b => b.Id);

        builder
            .Property(b => b.Name)
            .IsRequired();

        builder
            .HasIndex(b => b.Name);

        builder
            .HasIndex(b => new { b.SiteId, b.Name })
            .IsUnique();

        builder
            .Property(b => b.Description)
            .IsRequired(false);

        builder
            .HasOne(b => b.Site)
            .WithMany(b => b.Companies)
            .HasForeignKey(b => b.SiteId);

        builder
            .HasMany(b => b.Users)
            .WithMany(b => b.Companies);

        builder
            .Property(b => b.CreatedOn)
            .IsRequired();

        builder
            .Property(b => b.UpdatedOn)
            .IsRequired();
    }
}
