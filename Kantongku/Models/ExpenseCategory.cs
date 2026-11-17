using System.ComponentModel.DataAnnotations;

namespace Kantongku.Models;

/// <summary>Alokasi pengeluaran spesifik di dalam sebuah kelompok (mis. "Makan &amp; Minum" di kelompok "Primer").</summary>
public class ExpenseCategory
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public ExpenseGroup Group { get; set; } = null!;

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;
}
