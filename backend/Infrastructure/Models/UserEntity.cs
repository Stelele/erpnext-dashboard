using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class UserEntity : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(b => b.Id);

        builder
            .Property(b => b.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

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
            .Property(b => b.Role)
            .HasConversion<string>()
            .HasDefaultValue(Role.Viewer)
            .IsRequired();

        builder
            .Property(b => b.PasswordHash)
            .IsRequired(false);

        builder
            .Property(b => b.FailedLoginCount)
            .IsRequired();

        builder
            .Property(b => b.LockoutUntil)
            .IsRequired(false);

        builder
            .HasMany(b => b.Companies)
            .WithMany(b => b.Users);

        builder
            .HasMany(b => b.Sessions)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(b => b.PasswordResetTokens)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(b => b.CreatedOn)
            .IsRequired();

        builder
            .Property(b => b.UpdatedOn)
            .IsRequired();
    }
}
