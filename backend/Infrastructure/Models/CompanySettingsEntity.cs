using Domain.CompanySettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class CompanySettingsEntity : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("CompanySettings");

        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(e => e.DefaultIncomeAccountName)
            .IsRequired()
            .HasMaxLength(255);

        builder
            .Property(e => e.PrimaryColor)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder
            .Property(e => e.NeutralColor)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder
            .HasIndex(e => e.CompanyId)
            .IsUnique();

        builder
            .Property(e => e.CompanyId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
