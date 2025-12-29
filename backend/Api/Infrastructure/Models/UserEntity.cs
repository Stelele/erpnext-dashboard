using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class UserEntity : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasKey(b => b.Id);

        builder
            .Property(b => b.Name)
            .IsRequired();

        builder
            .Property(b => b.Email)
            .IsRequired();

        builder
            .HasIndex(b => b.Email)
            .IsUnique();

        builder
            .Property(b => b.Auth0UserId)
            .IsRequired(false);

        builder
            .HasMany(b => b.Companies)
            .WithMany(b => b.Users);

        builder
            .Property(b => b.CreatedOn)
            .IsRequired();

        builder
            .Property(b => b.UpdatedOn)
            .IsRequired();
    }
}
