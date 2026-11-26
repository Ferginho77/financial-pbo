using System.ComponentModel.DataAnnotations;

namespace Kantongku.Models;

public class Pocket : BaseEntity<int>
{
    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    /// <summary>Kunci warna aksen kartu: navy, amber, emerald, sky, rose, slate.</summary>
    [Required, MaxLength(20)]
    public string Color { get; set; } = "navy";

    [Required]
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser Owner { get; set; } = null!;

    /// <summary>Saldo berjalan. Hanya diubah lewat PocketService (UPDATE atomik di dalam DB transaction).</summary>
    public decimal Balance { get; set; }

    /// <summary>Opsional: target saldo/tabungan atau batas anggaran kantong.</summary>
    public decimal? TargetAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PocketMember> Members { get; set; } = new List<PocketMember>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public override string GetSummary() => $"{Name} (Saldo: {Balance:N0})";
}
