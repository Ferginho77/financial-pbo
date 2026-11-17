using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Kantongku.Data;

public static class DbInitializer
{
    /// <summary>
    /// Membuat database + seluruh tabel (Identity &amp; domain) bila belum ada, serta memastikan
    /// kolom tambahan pada instalasi lama ikut diterapkan secara idempoten.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                await EnsureSchemaUpgradesAsync(db);
                logger.LogInformation("Database siap.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Gagal menyiapkan database (percobaan {Attempt}/{Max}). Pastikan MySQL di XAMPP sudah Start. Mencoba lagi...", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }

    private static async Task EnsureSchemaUpgradesAsync(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync();
        }

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'Pockets'
              AND COLUMN_NAME = 'TargetAmount';";

        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
        if (!exists)
        {
            await using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE `Pockets` ADD COLUMN `TargetAmount` decimal(18,2) NULL AFTER `Balance`;";
            await alterCmd.ExecuteNonQueryAsync();
        }
    }
}
