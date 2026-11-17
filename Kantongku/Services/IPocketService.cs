using Kantongku.Models;
using Kantongku.Models.ViewModels;

namespace Kantongku.Services;

public interface IPocketService
{
    // ---- Query ----
    Task<DashboardViewModel> GetDashboardAsync(string userId, string? month = null);

    /// <summary>Peran user pada kantong, atau null jika bukan anggota.</summary>
    Task<PocketRole?> GetRoleAsync(string userId, int pocketId);

    /// <summary>Detail kantong + mutasi + anggota + opsi form. Null jika user bukan anggota.</summary>
    Task<PocketDetailsViewModel?> GetDetailsAsync(string userId, int pocketId, string? filter, string? search, string? month, int page);

    /// <summary>Data mutasi lengkap untuk ekspor CSV. Null jika user bukan anggota.</summary>
    Task<(string PocketName, List<TransactionRowViewModel> Rows)?> GetTransactionsForExportAsync(string userId, int pocketId, string? filter, string? search, string? month);

    Task<PocketFormViewModel?> GetForEditAsync(string userId, int pocketId);

    // ---- Kantong ----
    Task<ServiceResult> CreatePocketAsync(string userId, PocketFormViewModel model);
    Task<ServiceResult> UpdatePocketAsync(string userId, PocketFormViewModel model);
    Task<ServiceResult> DeletePocketAsync(string userId, int pocketId);

    // ---- Transaksi (atomic) ----
    Task<ServiceResult> AddIncomeAsync(string userId, int pocketId, IncomeInput input);
    Task<ServiceResult> AddExpenseAsync(string userId, int pocketId, ExpenseInput input);
    Task<ServiceResult> TransferAsync(string userId, int sourcePocketId, TransferInput input);
    Task<ServiceResult> EditTransactionAsync(string userId, int pocketId, TransactionEditInput input);
    Task<ServiceResult> DeleteTransactionAsync(string userId, int pocketId, long transactionId);

    // ---- Shared pocket ----
    Task<ServiceResult> InviteMemberAsync(string userId, int pocketId, InviteInput input);
    Task<ServiceResult> ChangeMemberRoleAsync(string userId, int pocketId, int memberId, PocketRole newRole);
    Task<ServiceResult> RemoveMemberAsync(string userId, int pocketId, int memberId);
    Task<ServiceResult> LeavePocketAsync(string userId, int pocketId);
}
