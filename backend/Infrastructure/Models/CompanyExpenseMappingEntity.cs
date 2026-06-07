using Domain.CompanyExpenseMappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class CompanyExpenseMappingEntity : IEntityTypeConfiguration<CompanyExpenseMapping>
{
    public void Configure(EntityTypeBuilder<CompanyExpenseMapping> builder)
    {
        builder.ToTable("CompanyExpenseMappings");

        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(e => e.ErpnextAccountName)
            .IsRequired()
            .HasMaxLength(255);

        builder
            .HasIndex(e => new { e.CompanyId, e.ExpenseTypeId })
            .IsUnique();

        builder
            .Property(e => e.CompanyId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(e => e.ExpenseTypeId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.ExpenseType)
            .WithMany()
            .HasForeignKey(e => e.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
