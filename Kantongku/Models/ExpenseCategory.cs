using System.ComponentModel.DataAnnotations;

namespace Kantongku.Models;

/// <summary>Alokasi pengeluaran spesifik di dalam sebuah kelompok (mis. "Makan &amp; Minum" di kelompok "Primer").</summary>
public class ExpenseCategory : BaseEntity<int>
{
    public int GroupId { get; set; }
    public ExpenseGroup Group { get; set; } = null!;

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    public override string GetSummary() => Name;
}
