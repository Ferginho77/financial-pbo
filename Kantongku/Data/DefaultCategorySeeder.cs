using Kantongku.Models;
using Microsoft.EntityFrameworkCore;

namespace Kantongku.Data;

/// <summary>
/// Seeder kategori default. Dipanggil otomatis saat user mendaftar, sehingga setiap user langsung
/// memiliki Kelompok &amp; Alokasi bawaan yang bisa diubah sesuai kebutuhan.
/// </summary>
public static class DefaultCategorySeeder
{
    public static readonly IReadOnlyList<(string Group, string[] Categories)> Defaults = new List<(string, string[])>
    {
        ("Primer", new[] { "Makan & Minum", "Transportasi", "Tagihan & Kos" }),
        ("Sekunder", new[] { "Hiburan", "Belanja", "Nongkrong" }),
        ("Tersier/Lainnya", new[] { "Lain-lain" })
    };

    public static async Task SeedForUserAsync(AppDbContext db, string userId, CancellationToken ct = default)
    {
        if (await db.ExpenseGroups.AnyAsync(g => g.UserId == userId, ct))
        {
            return;
        }

        var order = 1;
        foreach (var (groupName, categories) in Defaults)
        {
            db.ExpenseGroups.Add(new ExpenseGroup
            {
                UserId = userId,
                Name = groupName,
                SortOrder = order++,
                Categories = categories.Select(c => new ExpenseCategory { Name = c }).ToList()
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
