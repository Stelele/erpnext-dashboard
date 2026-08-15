using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class SessionEntity : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(b => b.Id);

        builder
            .Property(b => b.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(b => b.UserId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .HasIndex(b => b.TokenHash)
            .IsUnique();

        builder
            .Property(b => b.TokenHash)
            .IsRequired();

        builder
            .Property(b => b.LastUsedOn)
            .IsRequired();

        builder
            .Property(b => b.CreatedOn)
            .IsRequired();

        builder
            .Property(b => b.UpdatedOn)
            .IsRequired();
    }
}