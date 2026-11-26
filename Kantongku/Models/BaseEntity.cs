namespace Kantongku.Models;

/// <summary>Base class abstrak generik untuk seluruh entitas domain dengan tipe primary key <typeparamref name="TKey"/>.</summary>
public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;

    /// <summary>Ringkasan identitas dan nilai entitas untuk audit/logging.</summary>
    public abstract string GetSummary();
}
