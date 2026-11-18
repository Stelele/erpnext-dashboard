using Application.Abstractions;
using Domain.CompanyExpenseMappings;
using CompanySettingsEntity = Domain.CompanySettings.CompanySettings;
using Domain.Companies;
using Domain.Exceptions;
using Domain.Features;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class CreateCompanyCommandHandler(
    DashboardDbContext db
) : ICommandHandler<CreateCompanyCommand, Guid>
{
    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var companyExists = await db.Companies
            .AnyAsync(c => c.Name == request.Name && c.SiteId == request.SiteId, cancellationToken);

        if (companyExists)
            throw new DuplicateDomainMemberException("A Company with the same name already exists for this Site.");

        var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == request.SiteId, cancellationToken)
            ?? throw new KeyNotFoundException($"The Site referenced does not exist: {request.SiteId}");

        var company = Company.Create(site, request.Name, request.Description);

        await db.Companies.AddAsync(company, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await SeedDefaultExpenseMappings(company.Id, cancellationToken);
        await SeedDefaultCompanySettings(company.Id, cancellationToken);
        await AssignRecipesAsync(company.Id, request.RecipeIds, cancellationToken);

        return company.Id;
    }

    private async Task AssignRecipesAsync(Guid companyId, Guid[]? recipeIds, CancellationToken ct)
    {
        if (recipeIds is null || recipeIds.Length == 0)
            return;

        var distinct = recipeIds.Distinct().ToArray();

        var existing = await db.Recipes
            .Where(r => distinct.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ct);

        var unknown = distinct.Except(existing).ToArray();

        if (unknown.Length > 0)
            throw new ValidationException($"Unknown recipe ids: {string.Join(", ", unknown)}");

        foreach (var recipeId in distinct)
        {
            db.CompanyRecipes.Add(new CompanyRecipe
            {
                CompanyId = companyId,
                RecipeId = recipeId,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedDefaultExpenseMappings(Guid companyId, CancellationToken ct)
    {
        var expenseTypes = await db.ExpenseTypes.ToListAsync(ct);
        var defaultAccountNames = new Dictionary<string, string>
        {
            { "Administrative", "Administrative Expenses" },
            { "Consumables", "Consumables" },
            { "Entertainment/Owner", "Entertainment/Director's Expenses" },
            { "Marketing", "Marketing Expenses" },
            { "Other", "Miscellaneous Expenses" },
            { "Professional Fees", "Legal Expenses" },
            { "Utilities", "Utility Expenses" },
            { "Rent", "Office Rent" },
            { "Staff Canteen", "Canteen" },
            { "Staff Salary", "Salary" },
            { "Travel", "Travel Expenses" },
        };

        var mappings = expenseTypes.Select(et => new CompanyExpenseMapping
        {
            CompanyId = companyId,
            ExpenseTypeId = et.Id,
            ErpnextAccountName = defaultAccountNames.GetValueOrDefault(et.Name, et.Name),
        }).ToList();

        await db.CompanyExpenseMappings.AddRangeAsync(mappings, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedDefaultCompanySettings(Guid companyId, CancellationToken ct)
    {
        var settings = new CompanySettingsEntity
        {
            CompanyId = companyId,
            DefaultIncomeAccountName = "Sales",
        };

        db.CompanySettings.Add(settings);
        await db.SaveChangesAsync(ct);
    }
}
