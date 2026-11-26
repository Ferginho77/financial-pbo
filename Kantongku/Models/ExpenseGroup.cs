using System.ComponentModel.DataAnnotations;

namespace Kantongku.Models;

/// <summary>Kelompok kategori berdasarkan sifat kebutuhan (Primer, Sekunder, Tersier/Lainnya, ...). Milik masing-masing user.</summary>
public class ExpenseGroup : BaseEntity<int>
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<ExpenseCategory> Categories { get; set; } = new List<ExpenseCategory>();

    public override string GetSummary() => $"{Name} ({Categories.Count} alokasi)";
}
