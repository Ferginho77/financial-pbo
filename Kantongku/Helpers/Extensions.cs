using System.Security.Claims;
using Kantongku.Models;

namespace Kantongku.Helpers;

public static class ClaimsPrincipalExtensions
{
    public const string DisplayNameClaim = "DisplayName";

    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User belum terautentikasi.");

    public static string GetDisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(DisplayNameClaim) ?? user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name ?? "Pengguna";

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name ?? string.Empty;
}

public static class EnumLabels
{
    public static string Label(this PocketRole role) => role switch
    {
        PocketRole.Owner => "Owner",
        PocketRole.Spender => "Spender",
        _ => "Viewer"
    };

    public static string Description(this PocketRole role) => role switch
    {
        PocketRole.Owner => "Kontrol penuh atas kantong",
        PocketRole.Spender => "Boleh mencatat pengeluaran",
        _ => "Hanya melihat saldo & mutasi"
    };

    public static string Label(this TransactionType type) => type switch
    {
        TransactionType.Income => "Pemasukan",
        TransactionType.Expense => "Pengeluaran",
        TransactionType.TransferIn => "Transfer Masuk",
        _ => "Transfer Keluar"
    };
}

/// <summary>Palet warna grafik: Primer = navy, Sekunder = amber, Lainnya = slate, sisanya warna pendukung.</summary>
public static class ChartPalette
{
    private static readonly string[] Extra = { "#10B981", "#0EA5E9", "#F43F5E", "#14B8A6", "#6366F1", "#84CC16" };

    public static string ForGroup(string name, int index)
    {
        var n = name.Trim().ToLowerInvariant();
        if (n == "primer") return "#0F172A";
        if (n == "sekunder") return "#F59E0B";
        if (n.Contains("lain") || n.Contains("tersier") || n.Contains("tanpa")) return "#94A3B8";
        return Extra[index % Extra.Length];
    }
}
