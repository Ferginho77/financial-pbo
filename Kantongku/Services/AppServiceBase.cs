using Kantongku.Data;

namespace Kantongku.Services;

/// <summary>Base class abstrak untuk service domain yang berbagi konteks database dan helper sanitasi teks.</summary>
public abstract class AppServiceBase
{
    protected readonly AppDbContext _db;

    protected AppServiceBase(AppDbContext db)
    {
        _db = db;
    }

    protected abstract string ServiceName { get; }

    protected static string? CleanOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
