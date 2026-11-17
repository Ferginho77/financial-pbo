using System.ComponentModel.DataAnnotations;

namespace Kantongku.Models.ViewModels;

public class PocketFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nama kantong wajib diisi.")]
    [StringLength(80, ErrorMessage = "Nama kantong maksimal 80 karakter.")]
    [Display(Name = "Nama kantong")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Deskripsi maksimal 255 karakter.")]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; }

    [Range(1, 999_999_999_999d, ErrorMessage = "Target saldo harus lebih besar dari Rp 0.")]
    [Display(Name = "Target saldo")]
    public decimal? TargetAmount { get; set; }

    [Display(Name = "Warna")]
    public string Color { get; set; } = "navy";
}

public class IncomeInput
{
    [Required(ErrorMessage = "Nominal pemasukan wajib diisi.")]
    [Range(1, 999_999_999_999d, ErrorMessage = "Nominal harus lebih besar dari Rp 0.")]
    [Display(Name = "Nominal")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Sumber pemasukan wajib diisi.")]
    [StringLength(100, ErrorMessage = "Sumber maksimal 100 karakter.")]
    [Display(Name = "Sumber pemasukan")]
    public string Source { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tanggal wajib diisi.")]
    [DataType(DataType.Date)]
    [Display(Name = "Tanggal")]
    public DateTime? Date { get; set; } = DateTime.Today;

    [StringLength(255, ErrorMessage = "Catatan maksimal 255 karakter.")]
    [Display(Name = "Catatan")]
    public string? Note { get; set; }
}

public class ExpenseInput
{
    [Required(ErrorMessage = "Nominal pengeluaran wajib diisi.")]
    [Range(1, 999_999_999_999d, ErrorMessage = "Nominal harus lebih besar dari Rp 0.")]
    [Display(Name = "Nominal")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Nama pengeluaran wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama pengeluaran maksimal 100 karakter.")]
    [Display(Name = "Nama pengeluaran")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pilih alokasi pengeluaran.")]
    [Display(Name = "Alokasi")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Tanggal wajib diisi.")]
    [DataType(DataType.Date)]
    [Display(Name = "Tanggal")]
    public DateTime? Date { get; set; } = DateTime.Today;

    [StringLength(255, ErrorMessage = "Catatan maksimal 255 karakter.")]
    [Display(Name = "Catatan")]
    public string? Note { get; set; }
}

public class TransferInput
{
    [Required(ErrorMessage = "Pilih kantong tujuan.")]
    [Display(Name = "Kantong tujuan")]
    public int? DestinationPocketId { get; set; }

    [Required(ErrorMessage = "Nominal transfer wajib diisi.")]
    [Range(1, 999_999_999_999d, ErrorMessage = "Nominal harus lebih besar dari Rp 0.")]
    [Display(Name = "Nominal")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Tanggal wajib diisi.")]
    [DataType(DataType.Date)]
    [Display(Name = "Tanggal")]
    public DateTime? Date { get; set; } = DateTime.Today;

    [StringLength(255, ErrorMessage = "Catatan maksimal 255 karakter.")]
    [Display(Name = "Catatan")]
    public string? Note { get; set; }
}

public class TransactionEditInput
{
    public long TransactionId { get; set; }

    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "Nominal wajib diisi.")]
    [Range(1, 999_999_999_999d, ErrorMessage = "Nominal harus lebih besar dari Rp 0.")]
    [Display(Name = "Nominal")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Keterangan wajib diisi.")]
    [StringLength(100, ErrorMessage = "Maksimal 100 karakter.")]
    [Display(Name = "Keterangan")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Alokasi")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Tanggal wajib diisi.")]
    [DataType(DataType.Date)]
    [Display(Name = "Tanggal")]
    public DateTime? Date { get; set; } = DateTime.Today;

    [StringLength(255, ErrorMessage = "Catatan maksimal 255 karakter.")]
    [Display(Name = "Catatan")]
    public string? Note { get; set; }
}

public class InviteInput
{
    [Required(ErrorMessage = "Email anggota wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Hak akses")]
    public PocketRole Role { get; set; } = PocketRole.Spender;
}
