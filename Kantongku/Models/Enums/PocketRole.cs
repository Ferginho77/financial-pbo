namespace Kantongku.Models;

/// <summary>Hak akses anggota di dalam sebuah kantong. Nilai lebih besar = hak akses lebih luas.</summary>
public enum PocketRole
{
    /// <summary>Hanya melihat saldo dan mutasi.</summary>
    Viewer = 0,

    /// <summary>Melihat saldo + mencatat pengeluaran memakai dana kantong.</summary>
    Spender = 1,

    /// <summary>Kontrol penuh: edit, undang/hapus anggota, hapus kantong, pemasukan, transfer.</summary>
    Owner = 2
}
