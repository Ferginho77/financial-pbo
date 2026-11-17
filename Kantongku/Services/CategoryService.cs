using Kantongku.Data;
using Kantongku.Models;
using Kantongku.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Kantongku.Services;

public class CategoryService : ICategoryService
{
    private const int MaxNameLength = 60;

    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    private static ServiceResult? ValidateName(string? name, out string clean)
    {
        clean = (name ?? string.Empty).Trim();
        if (clean.Length == 0)
        {
            return ServiceResult.Fail("Nama wajib diisi.");
        }

        if (clean.Length > MaxNameLength)
        {
            return ServiceResult.Fail($"Nama maksimal {MaxNameLength} karakter.");
        }

        return null;
    }

    public async Task<List<ExpenseGroupViewModel>> GetGroupsAsync(string userId)
    {
        var groups = await _db.ExpenseGroups.AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Name)
            .Select(g => new ExpenseGroupViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Categories = g.Categories
                    .OrderBy(c => c.Name)
                    .Select(c => new ExpenseCategoryViewModel { Id = c.Id, Name = c.Name })
                    .ToList()
            })
            .ToListAsync();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var catTotals = await _db.Transactions.AsNoTracking()
            .Where(t => t.CreatedByUserId == userId
                        && t.Type == TransactionType.Expense
                        && t.CategoryId != null
                        && t.Date >= monthStart
                        && t.Date < nextMonth)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total);

        foreach (var g in groups)
        {
            foreach (var c in g.Categories)
            {
                if (catTotals.TryGetValue(c.Id, out var total))
                {
                    c.MonthTotal = total;
                }
            }

            g.MonthTotal = g.Categories.Sum(c => c.MonthTotal);
        }

        return groups;
    }

    public Task SeedDefaultsAsync(string userId) => DefaultCategorySeeder.SeedForUserAsync(_db, userId);

    // ---------------- Kelompok ----------------

    public async Task<ServiceResult> CreateGroupAsync(string userId, string name)
    {
        var invalid = ValidateName(name, out var clean);
        if (invalid is not null)
        {
            return invalid;
        }

        var lower = clean.ToLower();
        if (await _db.ExpenseGroups.AnyAsync(g => g.UserId == userId && g.Name.ToLower() == lower))
        {
            return ServiceResult.Fail("Kelompok dengan nama tersebut sudah ada.");
        }

        var nextOrder = (await _db.ExpenseGroups.Where(g => g.UserId == userId).MaxAsync(g => (int?)g.SortOrder) ?? 0) + 1;

        var group = new ExpenseGroup { UserId = userId, Name = clean, SortOrder = nextOrder };
        _db.ExpenseGroups.Add(group);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(group.Id);
    }

    public async Task<ServiceResult> RenameGroupAsync(string userId, int groupId, string name)
    {
        var invalid = ValidateName(name, out var clean);
        if (invalid is not null)
        {
            return invalid;
        }

        var group = await _db.ExpenseGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId);
        if (group is null)
        {
            return ServiceResult.NotFound();
        }

        var lower = clean.ToLower();
        if (await _db.ExpenseGroups.AnyAsync(g => g.UserId == userId && g.Name.ToLower() == lower && g.Id != groupId))
        {
            return ServiceResult.Fail("Kelompok dengan nama tersebut sudah ada.");
        }

        var categoryIds = await _db.ExpenseCategories
            .Where(c => c.GroupId == groupId)
            .Select(c => c.Id)
            .ToListAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();
        group.Name = clean;
        await _db.SaveChangesAsync();

        if (categoryIds.Count > 0)
        {
            await _db.Transactions
                .Where(t => t.CategoryId != null && categoryIds.Contains(t.CategoryId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.GroupName, clean));
        }

        await tx.CommitAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteGroupAsync(string userId, int groupId)
    {
        var group = await _db.ExpenseGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId);
        if (group is null)
        {
            return ServiceResult.NotFound();
        }

        _db.ExpenseGroups.Remove(group);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    // ---------------- Alokasi ----------------

    public async Task<ServiceResult> CreateCategoryAsync(string userId, int groupId, string name)
    {
        var invalid = ValidateName(name, out var clean);
        if (invalid is not null)
        {
            return invalid;
        }

        var groupExists = await _db.ExpenseGroups.AnyAsync(g => g.Id == groupId && g.UserId == userId);
        if (!groupExists)
        {
            return ServiceResult.NotFound();
        }

        var lower = clean.ToLower();
        if (await _db.ExpenseCategories.AnyAsync(c => c.GroupId == groupId && c.Name.ToLower() == lower))
        {
            return ServiceResult.Fail("Alokasi dengan nama tersebut sudah ada di kelompok ini.");
        }

        var category = new ExpenseCategory { GroupId = groupId, Name = clean };
        _db.ExpenseCategories.Add(category);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(category.Id);
    }

    public async Task<ServiceResult> RenameCategoryAsync(string userId, int categoryId, string name)
    {
        var invalid = ValidateName(name, out var clean);
        if (invalid is not null)
        {
            return invalid;
        }

        var category = await _db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.Group.UserId == userId);
        if (category is null)
        {
            return ServiceResult.NotFound();
        }

        var lower = clean.ToLower();
        if (await _db.ExpenseCategories.AnyAsync(c => c.GroupId == category.GroupId && c.Name.ToLower() == lower && c.Id != categoryId))
        {
            return ServiceResult.Fail("Alokasi dengan nama tersebut sudah ada di kelompok ini.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        category.Name = clean;
        await _db.SaveChangesAsync();

        await _db.Transactions
            .Where(t => t.CategoryId == categoryId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CategoryName, clean));

        await tx.CommitAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteCategoryAsync(string userId, int categoryId)
    {
        var category = await _db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.Group.UserId == userId);
        if (category is null)
        {
            return ServiceResult.NotFound();
        }

        _db.ExpenseCategories.Remove(category);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
