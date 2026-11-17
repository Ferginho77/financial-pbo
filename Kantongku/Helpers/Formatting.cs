using System.Globalization;

namespace Kantongku.Helpers;

/// <summary>Seluruh format angka & tanggal aplikasi memakai CultureInfo("id-ID").</summary>
public static class Formatting
{
    public static readonly CultureInfo Culture = CreateCulture();

    private static CultureInfo CreateCulture()
    {
        try
        {
            return CultureInfo.ReadOnly(new CultureInfo("id-ID"));
        }
        catch (CultureNotFoundException)
        {
            // Fallback bila ICU tidak tersedia (InvariantGlobalization): tiru pemisah ribuan id-ID.
            var fallback = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            fallback.NumberFormat.NumberGroupSeparator = ".";
            fallback.NumberFormat.NumberDecimalSeparator = ",";
            return CultureInfo.ReadOnly(fallback);
        }
    }

    /// <summary>Contoh: 1500000 =&gt; "Rp 1.500.000"; -25000 =&gt; "-Rp 25.000".</summary>
    public static string ToRupiah(this decimal amount)
    {
        var rounded = Math.Round(Math.Abs(amount), 0, MidpointRounding.AwayFromZero);
        var text = "Rp " + rounded.ToString("N0", Culture);
        return amount < 0 && rounded != 0 ? "-" + text : text;
    }

    public static string ToRupiah(this decimal? amount) => (amount ?? 0m).ToRupiah();

    /// <summary>Angka mentah tanpa pemisah untuk atribut value input (JS yang menambahkan titik ribuan).</summary>
    public static string ToInputValue(this decimal? amount) =>
        amount is > 0 ? Math.Round(amount.Value, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture) : string.Empty;

    public static string ToIdDate(this DateTime date) => date.ToString("dd MMM yyyy", Culture);

    public static string ToIdLongDate(this DateTime date) => date.ToString("dddd, dd MMMM yyyy", Culture);

    public static string ToIdMonth(this DateTime date) => date.ToString("MMMM yyyy", Culture);

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Split(new[] { ' ', '@', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0][..1].ToUpperInvariant();
        }

        return (parts[0][..1] + parts[1][..1]).ToUpperInvariant();
    }
}
