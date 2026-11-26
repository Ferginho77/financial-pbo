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
    public ResultKind Kind { get; protected init; }
    public string? Error { get; protected init; }

    /// <summary>Nama properti form yang terkait error (opsional) untuk ModelState.</summary>
    public string? Field { get; protected init; }

    /// <summary>Id entitas baru (jika relevan).</summary>
    public int? EntityId { get; protected init; }

    public bool Succeeded => Kind == ResultKind.Success;

    public static ServiceResult Ok(int? entityId = null) => new() { Kind = ResultKind.Success, EntityId = entityId };

    public static ServiceResult Fail(string error, string? field = null) =>
        new() { Kind = ResultKind.Validation, Error = error, Field = field };

    public static ServiceResult NotFound() =>
        new() { Kind = ResultKind.NotFound, Error = "Data tidak ditemukan." };

    public static ServiceResult Forbidden(string? error = null) =>
        new() { Kind = ResultKind.Forbidden, Error = error ?? "Anda tidak memiliki akses untuk tindakan ini." };
}

/// <summary>Varian generik dari <see cref="ServiceResult"/> yang mengembalikan payload data bertipe <typeparamref name="T"/>.</summary>
public sealed class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private init; }

    public static ServiceResult<T> Ok(T data, int? entityId = null) =>
        new() { Kind = ResultKind.Success, Data = data, EntityId = entityId };

    public new static ServiceResult<T> Fail(string error, string? field = null) =>
        new() { Kind = ResultKind.Validation, Error = error, Field = field };
}
