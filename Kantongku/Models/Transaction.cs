using System.ComponentModel.DataAnnotations;

namespace Kantongku.Models;

public class Transaction
{
    public long Id { get; set; }

    public int PocketId { get; set; }
    public Pocket Pocket { get; set; } = null!;

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Nama transaksi: "Makan Siang", "Gaji Bulanan", "Transfer ke Pengeluaran Bulanan", dst.</summary>
    [Required, MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Sumber pemasukan (khusus Income).</summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    [MaxLength(255)]
    public string? Note { get; set; }

    /// <summary>Tanggal transaksi (bagian tanggal saja).</summary>
    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Audit trail: user yang mencatat transaksi.</summary>
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;

    public int? CategoryId { get; set; }
    public ExpenseCategory? Category { get; set; }

    /// <summary>Snapshot nama kelompok &amp; alokasi, agar riwayat/analitik tetap utuh walau kategori dihapus.</summary>
    [MaxLength(60)]
    public string? GroupName { get; set; }

    [MaxLength(60)]
    public string? CategoryName { get; set; }

    /// <summary>Untuk transfer: kantong lawan transaksi (tanpa FK agar tidak membuat cascade ganda).</summary>
    public int? RelatedPocketId { get; set; }

    [MaxLength(80)]
    public string? RelatedPocketName { get; set; }

    /// <summary>Pasangan TransferOut/TransferIn memakai GUID yang sama.</summary>
    public Guid? TransferGroupId { get; set; }

    public bool IsCredit => Type is TransactionType.Income or TransactionType.TransferIn;
}
