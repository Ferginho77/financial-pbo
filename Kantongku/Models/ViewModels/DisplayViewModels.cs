using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kantongku.Models.ViewModels;

public class TransactionRowViewModel
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Source { get; set; }
    public string? Note { get; set; }
    public int? CategoryId { get; set; }
    public string? GroupName { get; set; }
    public string? CategoryName { get; set; }
    public string? RelatedPocketName { get; set; }
    public string PocketName { get; set; } = string.Empty;
    public int PocketId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string? ActorEmail { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public bool IsCredit => Type is TransactionType.Income or TransactionType.TransferIn;

    public string? CategoryLabel =>
        string.IsNullOrEmpty(CategoryName) ? null : $"[{GroupName ?? "Lainnya"}] {CategoryName}";

    public string ActorLabel => string.IsNullOrWhiteSpace(ActorName) ? (ActorEmail ?? "-") : ActorName;
}

public class PocketCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "navy";
    public decimal Balance { get; set; }
    public decimal? TargetAmount { get; set; }
    public int MemberCount { get; set; }
    public PocketRole Role { get; set; }

    public bool IsShared => MemberCount > 1;
    public bool CanIncome => Role == PocketRole.Owner;
    public bool CanSpend => Role is PocketRole.Owner or PocketRole.Spender;
    public bool HasTarget => TargetAmount is > 0;
    public int TargetPercent => TargetAmount is > 0
        ? (int)Math.Clamp(Math.Round(Balance / TargetAmount.Value * 100m, 0), 0, 100)
        : 0;
}

public class ChartSlice
{
    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class DashboardViewModel
{
    public List<PocketCardViewModel> Pockets { get; set; } = new();
    public decimal TotalBalance { get; set; }
    public decimal PersonalBalance { get; set; }
    public decimal SharedBalance { get; set; }
    public decimal MonthIncome { get; set; }
    public decimal MonthExpense { get; set; }
    public decimal NetCashflow => MonthIncome - MonthExpense;
    public DateTime Month { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    public string SelectedMonthKey => Month.ToString("yyyy-MM");
    public string PrevMonthKey => Month.AddMonths(-1).ToString("yyyy-MM");
    public string NextMonthKey => Month.AddMonths(1).ToString("yyyy-MM");
    public bool CanGoNextMonth => Month < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    /// <summary>Pengeluaran bulan terpilih per kelompok (Primer/Sekunder/Lainnya) - Doughnut chart.</summary>
    public List<ChartSlice> GroupBreakdown { get; set; } = new();

    /// <summary>Pengeluaran bulan terpilih per alokasi - Bar chart.</summary>
    public List<ChartSlice> CategoryBreakdown { get; set; } = new();

    public List<TransactionRowViewModel> Recent { get; set; } = new();
}

public class MemberViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PocketRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsCurrentUser { get; set; }

    public string Label => string.IsNullOrWhiteSpace(Name) ? Email : Name;
}

public class PocketDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "navy";
    public decimal Balance { get; set; }
    public decimal? TargetAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string OwnerLabel { get; set; } = string.Empty;

    public PocketRole Role { get; set; }
    public bool CanManage => Role == PocketRole.Owner;
    public bool CanSpend => Role is PocketRole.Owner or PocketRole.Spender;
    public bool IsShared => Members.Count > 1;
    public bool HasTarget => TargetAmount is > 0;
    public int TargetPercent => TargetAmount is > 0
        ? (int)Math.Clamp(Math.Round(Balance / TargetAmount.Value * 100m, 0), 0, 100)
        : 0;

    public decimal MonthIncome { get; set; }
    public decimal MonthExpense { get; set; }

    public List<MemberViewModel> Members { get; set; } = new();

    // Mutasi + filter + pencarian + paginasi
    public List<TransactionRowViewModel> Transactions { get; set; } = new();
    public string Filter { get; set; } = "all";
    public string? SearchQuery { get; set; }
    public string? SelectedMonth { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }

    // Form
    public ExpenseInput Expense { get; set; } = new();
    public IncomeInput Income { get; set; } = new();
    public TransferInput Transfer { get; set; } = new();
    public TransactionEditInput EditTransaction { get; set; } = new();
    public InviteInput Invite { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; set; } = new();
    public List<SelectListItem> TransferTargets { get; set; } = new();

    /// <summary>Dialog yang otomatis dibuka (income / expense / transfer / editTx / invite).</summary>
    public string? OpenDialog { get; set; }
}

public class ExpenseCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthTotal { get; set; }
}

public class ExpenseGroupViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthTotal { get; set; }
    public List<ExpenseCategoryViewModel> Categories { get; set; } = new();
}

public class ErrorViewModel
{
    public int Code { get; set; } = 500;
    public string Title { get; set; } = "Terjadi kesalahan";
    public string Message { get; set; } = "Maaf, terjadi kesalahan tak terduga. Silakan coba lagi.";
    public string? RequestId { get; set; }
}
