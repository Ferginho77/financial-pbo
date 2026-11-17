namespace Kantongku.Services;

public enum ResultKind
{
    Success,
    Validation,
    NotFound,
    Forbidden
}

/// <summary>Hasil operasi service: sukses, gagal validasi bisnis, tidak ditemukan, atau tidak punya hak akses.</summary>
public class ServiceResult
{
    public ResultKind Kind { get; private init; }
    public string? Error { get; private init; }

    /// <summary>Nama properti form yang terkait error (opsional) untuk ModelState.</summary>
    public string? Field { get; private init; }

    /// <summary>Id entitas baru (jika relevan).</summary>
    public int? EntityId { get; private init; }

    public bool Succeeded => Kind == ResultKind.Success;

    public static ServiceResult Ok(int? entityId = null) => new() { Kind = ResultKind.Success, EntityId = entityId };

    public static ServiceResult Fail(string error, string? field = null) =>
        new() { Kind = ResultKind.Validation, Error = error, Field = field };

    public static ServiceResult NotFound() =>
        new() { Kind = ResultKind.NotFound, Error = "Data tidak ditemukan." };

    public static ServiceResult Forbidden(string? error = null) =>
        new() { Kind = ResultKind.Forbidden, Error = error ?? "Anda tidak memiliki akses untuk tindakan ini." };
}
