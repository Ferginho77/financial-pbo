using System.Globalization;
using Kantongku.Data;
using Kantongku.Helpers;
using Kantongku.Models;
using Kantongku.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Kantongku.Services;

public class PocketService : AppServiceBase, IPocketService
{
    public const int PageSize = 15;

    private static readonly string[] AllowedColors = { "navy", "amber", "emerald", "sky", "rose", "slate" };

    public PocketService(AppDbContext db) : base(db)
    {
    }

    protected override string ServiceName => nameof(PocketService);

    // =====================================================================
    // Helper privat
    // =====================================================================

    private static bool CanSpend(PocketRole? role) => role is PocketRole.Owner or PocketRole.Spender;

    private static string NormalizeColor(string? color)
    {
        var c = (color ?? string.Empty).Trim().ToLowerInvariant();
        return AllowedColors.Contains(c) ? c : "navy";
    }

    private static decimal NormalizeAmount(decimal? amount) =>
        Math.Round(amount.GetValueOrDefault(), 0, MidpointRounding.AwayFromZero);

    private static decimal? NormalizeTarget(decimal? target)
    {
        if (target is null)
        {
            return null;
        }

        var rounded = Math.Round(target.Value, 0, MidpointRounding.AwayFromZero);
        return rounded > 0 ? rounded : null;
    }

    private static DateTime ResolveMonthStart(string? monthKey)
    {
        var today = DateTime.Today;
        var currentMonth = new DateTime(today.Year, today.Month, 1);

        if (!string.IsNullOrWhiteSpace(monthKey) &&
            DateTime.TryParseExact(monthKey.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            var selected = new DateTime(parsed.Year, parsed.Month, 1);
            if (selected <= currentMonth && selected.Year >= 2000)
            {
                return selected;
            }
        }

        return currentMonth;
    }

    private static bool TryParseMonth(string? monthKey, out DateTime monthStart)
    {
        monthStart = default;
        if (string.IsNullOrWhiteSpace(monthKey))
        {
            return false;
        }

        if (DateTime.TryParseExact(monthKey.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) &&
            parsed.Year >= 2000 && parsed.Year <= 2100)
        {
            monthStart = new DateTime(parsed.Year, parsed.Month, 1);
            return true;
        }

        return false;
    }

    private static ServiceResult? ValidateDate(DateTime? date, string fieldName, out DateTime result)
    {
        result = DateTime.Today;
        if (date is null)
        {
            return ServiceResult.Fail("Tanggal wajib diisi.", fieldName);
        }

        result = date.Value.Date;
        if (result > DateTime.Today)
        {
            return ServiceResult.Fail("Tanggal tidak boleh di masa depan.", fieldName);
        }

        if (result < new DateTime(2000, 1, 1))
        {
            return ServiceResult.Fail("Tanggal tidak valid.", fieldName);
        }

        return null;
    }

    private static string InsufficientMessage(decimal balance) =>
        $"Nominal melebihi sisa saldo kantong ({balance.ToRupiah()}).";

    private IQueryable<Transaction> BuildMutationQuery(int pocketId, string normalizedFilter, string? search, DateTime? monthStart)
    {
        var query = _db.Transactions.AsNoTracking().Where(t => t.PocketId == pocketId);

        query = normalizedFilter switch
        {
            "income" => query.Where(t => t.Type == TransactionType.Income),
            "expense" => query.Where(t => t.Type == TransactionType.Expense),
            "transfer" => query.Where(t => t.Type == TransactionType.TransferIn || t.Type == TransactionType.TransferOut),
            _ => query
        };

        if (monthStart.HasValue)
        {
            var next = monthStart.Value.AddMonths(1);
            query = query.Where(t => t.Date >= monthStart.Value && t.Date < next);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(term) ||
                (t.Note != null && t.Note.ToLower().Contains(term)) ||
                (t.CategoryName != null && t.CategoryName.ToLower().Contains(term)) ||
                (t.GroupName != null && t.GroupName.ToLower().Contains(term)) ||
                t.CreatedBy.DisplayName.ToLower().Contains(term) ||
                (t.CreatedBy.Email != null && t.CreatedBy.Email.ToLower().Contains(term)));
        }

        return query;
    }

    private static string NormalizeFilter(string? filter)
    {
        var normalized = (filter ?? "all").Trim().ToLowerInvariant();
        return normalized is "income" or "expense" or "transfer" ? normalized : "all";
    }

    // =====================================================================
    // Query
    // =====================================================================

    public async Task<PocketRole?> GetRoleAsync(string userId, int pocketId)
    {
        return await _db.PocketMembers.AsNoTracking()
            .Where(m => m.PocketId == pocketId && m.UserId == userId)
            .Select(m => (PocketRole?)m.Role)
            .FirstOrDefaultAsync();
    }

    public async Task<DashboardViewModel> GetDashboardAsync(string userId, string? month = null)
    {
        var pockets = await _db.PocketMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.Pocket.Name)
            .Select(m => new PocketCardViewModel
            {
                Id = m.PocketId,
                Name = m.Pocket.Name,
                Description = m.Pocket.Description,
                Color = m.Pocket.Color,
                Balance = m.Pocket.Balance,
                TargetAmount = m.Pocket.TargetAmount,
                MemberCount = m.Pocket.Members.Count,
                Role = m.Role
            })
            .ToListAsync();

        var monthStart = ResolveMonthStart(month);
        var nextMonth = monthStart.AddMonths(1);
        var pocketIds = pockets.Select(p => p.Id).ToList();

        var vm = new DashboardViewModel
        {
            Pockets = pockets,
            Month = monthStart,
            TotalBalance = pockets.Sum(p => p.Balance),
            PersonalBalance = pockets.Where(p => !p.IsShared).Sum(p => p.Balance),
            SharedBalance = pockets.Where(p => p.IsShared).Sum(p => p.Balance)
        };

        if (pocketIds.Count == 0)
        {
            return vm;
        }

        var monthTx = _db.Transactions.AsNoTracking()
            .Where(t => pocketIds.Contains(t.PocketId) && t.Date >= monthStart && t.Date < nextMonth);

        vm.MonthIncome = await monthTx.Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        vm.MonthExpense = await monthTx.Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var rows = await monthTx.Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.GroupName, t.CategoryName })
            .Select(g => new { g.Key.GroupName, g.Key.CategoryName, Total = g.Sum(x => x.Amount) })
            .ToListAsync();

        vm.GroupBreakdown = rows
            .GroupBy(r => r.GroupName ?? "Tanpa Kategori")
            .Select(g => new ChartSlice { Label = g.Key, Group = g.Key, Amount = g.Sum(x => x.Total) })
            .OrderByDescending(s => s.Amount)
            .ToList();

        vm.CategoryBreakdown = rows
            .GroupBy(r => new { Group = r.GroupName ?? "Tanpa Kategori", Category = r.CategoryName ?? "Lainnya" })
            .Select(g => new ChartSlice { Label = g.Key.Category, Group = g.Key.Group, Amount = g.Sum(x => x.Total) })
            .OrderByDescending(s => s.Amount)
            .ToList();

        vm.Recent = await _db.Transactions.AsNoTracking()
            .Where(t => pocketIds.Contains(t.PocketId))
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(8)
            .Select(t => new TransactionRowViewModel
            {
                Id = t.Id,
                Date = t.Date,
                Type = t.Type,
                Title = t.Title,
                Amount = t.Amount,
                Source = t.Source,
                Note = t.Note,
                CategoryId = t.CategoryId,
                GroupName = t.GroupName,
                CategoryName = t.CategoryName,
                RelatedPocketName = t.RelatedPocketName,
                PocketId = t.PocketId,
                PocketName = t.Pocket.Name,
                CreatedByUserId = t.CreatedByUserId,
                ActorName = t.CreatedBy.DisplayName,
                ActorEmail = t.CreatedBy.Email
            })
            .ToListAsync();

        return vm;
    }

    public async Task<PocketDetailsViewModel?> GetDetailsAsync(string userId, int pocketId, string? filter, string? search, string? month, int page)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return null;
        }

        var pocket = await _db.Pockets.AsNoTracking()
            .Where(p => p.Id == pocketId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Color,
                p.Balance,
                p.TargetAmount,
                p.CreatedAt,
                OwnerName = p.Owner.DisplayName,
                OwnerEmail = p.Owner.Email
            })
            .FirstOrDefaultAsync();

        if (pocket is null)
        {
            return null;
        }

        var normalizedFilter = NormalizeFilter(filter);
        DateTime? selectedMonthStart = TryParseMonth(month, out var parsedMonth) ? parsedMonth : null;
        var cleanSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var query = BuildMutationQuery(pocketId, normalizedFilter, cleanSearch, selectedMonthStart);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        var currentPage = Math.Clamp(page, 1, totalPages);

        var ownerPocketIds = role == PocketRole.Owner
            ? (await _db.PocketMembers.AsNoTracking()
                .Where(m => m.UserId == userId && m.Role == PocketRole.Owner)
                .Select(m => m.PocketId)
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        var rawTx = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(t => new
            {
                t.Id,
                t.Date,
                t.Type,
                t.Title,
                t.Amount,
                t.Source,
                t.Note,
                t.CategoryId,
                t.GroupName,
                t.CategoryName,
                t.RelatedPocketId,
                t.RelatedPocketName,
                t.PocketId,
                PocketName = t.Pocket.Name,
                t.CreatedByUserId,
                ActorName = t.CreatedBy.DisplayName,
                ActorEmail = t.CreatedBy.Email
            })
            .ToListAsync();

        var transactions = rawTx.Select(t =>
        {
            var isTransfer = t.Type is TransactionType.TransferIn or TransactionType.TransferOut;
            var canEdit = !isTransfer && (role == PocketRole.Owner ||
                (role == PocketRole.Spender && t.Type == TransactionType.Expense && t.CreatedByUserId == userId));
            var canDelete = isTransfer
                ? role == PocketRole.Owner && t.RelatedPocketId.HasValue && ownerPocketIds.Contains(t.RelatedPocketId.Value)
                : (role == PocketRole.Owner ||
                   (role == PocketRole.Spender && t.Type == TransactionType.Expense && t.CreatedByUserId == userId));

            return new TransactionRowViewModel
            {
                Id = t.Id,
                Date = t.Date,
                Type = t.Type,
                Title = t.Title,
                Amount = t.Amount,
                Source = t.Source,
                Note = t.Note,
                CategoryId = t.CategoryId,
                GroupName = t.GroupName,
                CategoryName = t.CategoryName,
                RelatedPocketName = t.RelatedPocketName,
                PocketId = t.PocketId,
                PocketName = t.PocketName,
                CreatedByUserId = t.CreatedByUserId,
                ActorName = t.ActorName,
                ActorEmail = t.ActorEmail,
                CanEdit = canEdit,
                CanDelete = canDelete
            };
        }).ToList();

        var members = await _db.PocketMembers.AsNoTracking()
            .Where(m => m.PocketId == pocketId)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new MemberViewModel
            {
                Id = m.Id,
                UserId = m.UserId,
                Name = m.User.DisplayName,
                Email = m.User.Email ?? string.Empty,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                IsCurrentUser = m.UserId == userId
            })
            .ToListAsync();

        var today = DateTime.Today;
        var currentMonthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = currentMonthStart.AddMonths(1);
        var monthTx = _db.Transactions.AsNoTracking()
            .Where(t => t.PocketId == pocketId && t.Date >= currentMonthStart && t.Date < nextMonth);

        var monthIncome = await monthTx.Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        var monthExpense = await monthTx.Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var vm = new PocketDetailsViewModel
        {
            Id = pocket.Id,
            Name = pocket.Name,
            Description = pocket.Description,
            Color = pocket.Color,
            Balance = pocket.Balance,
            TargetAmount = pocket.TargetAmount,
            CreatedAt = pocket.CreatedAt.ToLocalTime(),
            OwnerLabel = string.IsNullOrWhiteSpace(pocket.OwnerName) ? (pocket.OwnerEmail ?? "-") : pocket.OwnerName,
            Role = role.Value,
            MonthIncome = monthIncome,
            MonthExpense = monthExpense,
            Members = members,
            Transactions = transactions,
            Filter = normalizedFilter,
            SearchQuery = cleanSearch,
            SelectedMonth = selectedMonthStart?.ToString("yyyy-MM"),
            Page = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        if (CanSpend(role))
        {
            var categories = await _db.ExpenseCategories.AsNoTracking()
                .Where(c => c.Group.UserId == userId)
                .OrderBy(c => c.Group.SortOrder)
                .ThenBy(c => c.Group.Name)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, Group = c.Group.Name, c.Name })
                .ToListAsync();

            vm.CategoryOptions = categories
                .Select(c => new SelectListItem($"[{c.Group}] {c.Name}", c.Id.ToString()))
                .ToList();
        }

        if (role == PocketRole.Owner)
        {
            var targets = await _db.PocketMembers.AsNoTracking()
                .Where(m => m.UserId == userId
                            && m.PocketId != pocketId
                            && (m.Role == PocketRole.Owner || m.Role == PocketRole.Spender))
                .OrderBy(m => m.Pocket.Name)
                .Select(m => new { m.PocketId, m.Pocket.Name, m.Pocket.Balance })
                .ToListAsync();

            vm.TransferTargets = targets
                .Select(t => new SelectListItem($"{t.Name} • {t.Balance.ToRupiah()}", t.PocketId.ToString()))
                .ToList();
        }

        return vm;
    }

    public async Task<(string PocketName, List<TransactionRowViewModel> Rows)?> GetTransactionsForExportAsync(
        string userId, int pocketId, string? filter, string? search, string? month)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return null;
        }

        var pocketName = await _db.Pockets.AsNoTracking()
            .Where(p => p.Id == pocketId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync();

        if (pocketName is null)
        {
            return null;
        }

        var normalizedFilter = NormalizeFilter(filter);
        DateTime? selectedMonthStart = TryParseMonth(month, out var parsedMonth) ? parsedMonth : null;
        var cleanSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var rows = await BuildMutationQuery(pocketId, normalizedFilter, cleanSearch, selectedMonthStart)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Select(t => new TransactionRowViewModel
            {
                Id = t.Id,
                Date = t.Date,
                Type = t.Type,
                Title = t.Title,
                Amount = t.Amount,
                Source = t.Source,
                Note = t.Note,
                GroupName = t.GroupName,
                CategoryName = t.CategoryName,
                RelatedPocketName = t.RelatedPocketName,
                PocketId = t.PocketId,
                PocketName = pocketName,
                ActorName = t.CreatedBy.DisplayName,
                ActorEmail = t.CreatedBy.Email
            })
            .ToListAsync();

        return (pocketName, rows);
    }

    public async Task<PocketFormViewModel?> GetForEditAsync(string userId, int pocketId)
    {
        return await _db.Pockets.AsNoTracking()
            .Where(p => p.Id == pocketId && p.OwnerId == userId)
            .Select(p => new PocketFormViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                TargetAmount = p.TargetAmount,
                Color = p.Color
            })
            .FirstOrDefaultAsync();
    }

    // =====================================================================
    // Kantong
    // =====================================================================

    public async Task<ServiceResult> CreatePocketAsync(string userId, PocketFormViewModel model)
    {
        var name = (model.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return ServiceResult.Fail("Nama kantong wajib diisi.", nameof(model.Name));
        }

        var lower = name.ToLower();
        var duplicate = await _db.Pockets.AnyAsync(p => p.OwnerId == userId && p.Name.ToLower() == lower);
        if (duplicate)
        {
            return ServiceResult.Fail("Anda sudah memiliki kantong dengan nama yang sama.", nameof(model.Name));
        }

        var pocket = new Pocket
        {
            Name = name,
            Description = CleanOptional(model.Description),
            TargetAmount = NormalizeTarget(model.TargetAmount),
            Color = NormalizeColor(model.Color),
            OwnerId = userId,
            Balance = 0m,
            CreatedAt = DateTime.UtcNow
        };

        pocket.Members.Add(new PocketMember
        {
            UserId = userId,
            Role = PocketRole.Owner,
            JoinedAt = DateTime.UtcNow
        });

        _db.Pockets.Add(pocket);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(pocket.Id);
    }

    public async Task<ServiceResult> UpdatePocketAsync(string userId, PocketFormViewModel model)
    {
        var role = await GetRoleAsync(userId, model.Id);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        if (role != PocketRole.Owner)
        {
            return ServiceResult.Forbidden("Hanya Owner yang dapat mengubah kantong.");
        }

        var name = (model.Name ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return ServiceResult.Fail("Nama kantong wajib diisi.", nameof(model.Name));
        }

        var lower = name.ToLower();
        var duplicate = await _db.Pockets.AnyAsync(p => p.OwnerId == userId && p.Name.ToLower() == lower && p.Id != model.Id);
        if (duplicate)
        {
            return ServiceResult.Fail("Anda sudah memiliki kantong dengan nama yang sama.", nameof(model.Name));
        }

        var pocket = await _db.Pockets.FirstAsync(p => p.Id == model.Id);
        pocket.Name = name;
        pocket.Description = CleanOptional(model.Description);
        pocket.TargetAmount = NormalizeTarget(model.TargetAmount);
        pocket.Color = NormalizeColor(model.Color);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok(pocket.Id);
    }

    public async Task<ServiceResult> DeletePocketAsync(string userId, int pocketId)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        if (role != PocketRole.Owner)
        {
            return ServiceResult.Forbidden("Hanya Owner yang dapat menghapus kantong.");
        }

        await _db.Pockets.Where(p => p.Id == pocketId).ExecuteDeleteAsync();
        return ServiceResult.Ok();
    }

    // =====================================================================
    // Transaksi (semua perubahan saldo dilakukan atomik di dalam DB transaction)
    // =====================================================================

    public async Task<ServiceResult> AddIncomeAsync(string userId, int pocketId, IncomeInput input)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        if (role != PocketRole.Owner)
        {
            return ServiceResult.Forbidden("Hanya Owner yang dapat mencatat pemasukan.");
        }

        var amount = NormalizeAmount(input.Amount);
        if (amount <= 0)
        {
            return ServiceResult.Fail("Nominal harus lebih besar dari Rp 0.", nameof(input.Amount));
        }

        var source = (input.Source ?? string.Empty).Trim();
        if (source.Length == 0)
        {
            return ServiceResult.Fail("Sumber pemasukan wajib diisi.", nameof(input.Source));
        }

        var dateError = ValidateDate(input.Date, nameof(input.Date), out var date);
        if (dateError is not null)
        {
            return dateError;
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var affected = await _db.Pockets
            .Where(p => p.Id == pocketId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance + amount));

        if (affected == 0)
        {
            await tx.RollbackAsync();
            return ServiceResult.NotFound();
        }

        _db.Transactions.Add(new Transaction
        {
            PocketId = pocketId,
            Type = TransactionType.Income,
            Amount = amount,
            Title = source,
            Source = source,
            Note = CleanOptional(input.Note),
            Date = date,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AddExpenseAsync(string userId, int pocketId, ExpenseInput input)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        if (!CanSpend(role))
        {
            return ServiceResult.Forbidden("Anda hanya memiliki akses melihat (Viewer) di kantong ini.");
        }

        var amount = NormalizeAmount(input.Amount);
        if (amount <= 0)
        {
            return ServiceResult.Fail("Nominal harus lebih besar dari Rp 0.", nameof(input.Amount));
        }

        var title = (input.Title ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            return ServiceResult.Fail("Nama pengeluaran wajib diisi.", nameof(input.Title));
        }

        var dateError = ValidateDate(input.Date, nameof(input.Date), out var date);
        if (dateError is not null)
        {
            return dateError;
        }

        var category = await _db.ExpenseCategories.AsNoTracking()
            .Where(c => c.Id == input.CategoryId && c.Group.UserId == userId)
            .Select(c => new { c.Id, c.Name, GroupName = c.Group.Name })
            .FirstOrDefaultAsync();

        if (category is null)
        {
            return ServiceResult.Fail("Alokasi pengeluaran tidak valid.", nameof(input.CategoryId));
        }

        var currentBalance = await _db.Pockets.AsNoTracking()
            .Where(p => p.Id == pocketId)
            .Select(p => p.Balance)
            .FirstAsync();

        if (amount > currentBalance)
        {
            return ServiceResult.Fail(InsufficientMessage(currentBalance), nameof(input.Amount));
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var affected = await _db.Pockets
            .Where(p => p.Id == pocketId && p.Balance >= amount)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance - amount));

        if (affected == 0)
        {
            await tx.RollbackAsync();
            var latest = await _db.Pockets.AsNoTracking().Where(p => p.Id == pocketId).Select(p => p.Balance).FirstAsync();
            return ServiceResult.Fail(InsufficientMessage(latest), nameof(input.Amount));
        }

        _db.Transactions.Add(new Transaction
        {
            PocketId = pocketId,
            Type = TransactionType.Expense,
            Amount = amount,
            Title = title,
            Note = CleanOptional(input.Note),
            Date = date,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CategoryId = category.Id,
            CategoryName = category.Name,
            GroupName = category.GroupName
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> TransferAsync(string userId, int sourcePocketId, TransferInput input)
    {
        var sourceRole = await GetRoleAsync(userId, sourcePocketId);
        if (sourceRole is null)
        {
            return ServiceResult.NotFound();
        }

        if (sourceRole != PocketRole.Owner)
        {
            return ServiceResult.Forbidden("Hanya Owner yang dapat mentransfer dana dari kantong ini.");
        }

        var destinationId = input.DestinationPocketId.GetValueOrDefault();
        if (destinationId == 0)
        {
            return ServiceResult.Fail("Pilih kantong tujuan.", nameof(input.DestinationPocketId));
        }

        if (destinationId == sourcePocketId)
        {
            return ServiceResult.Fail("Kantong tujuan harus berbeda dengan kantong asal.", nameof(input.DestinationPocketId));
        }

        var destinationRole = await GetRoleAsync(userId, destinationId);
        if (!CanSpend(destinationRole))
        {
            return ServiceResult.Fail("Anda tidak memiliki akses ke kantong tujuan tersebut.", nameof(input.DestinationPocketId));
        }

        var amount = NormalizeAmount(input.Amount);
        if (amount <= 0)
        {
            return ServiceResult.Fail("Nominal harus lebih besar dari Rp 0.", nameof(input.Amount));
        }

        var dateError = ValidateDate(input.Date, nameof(input.Date), out var date);
        if (dateError is not null)
        {
            return dateError;
        }

        var pockets = await _db.Pockets.AsNoTracking()
            .Where(p => p.Id == sourcePocketId || p.Id == destinationId)
            .Select(p => new { p.Id, p.Name, p.Balance })
            .ToListAsync();

        var source = pockets.FirstOrDefault(p => p.Id == sourcePocketId);
        var destination = pockets.FirstOrDefault(p => p.Id == destinationId);
        if (source is null || destination is null)
        {
            return ServiceResult.NotFound();
        }

        if (amount > source.Balance)
        {
            return ServiceResult.Fail(InsufficientMessage(source.Balance), nameof(input.Amount));
        }

        var note = CleanOptional(input.Note);
        var groupId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var debited = await _db.Pockets
            .Where(p => p.Id == sourcePocketId && p.Balance >= amount)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance - amount));

        if (debited == 0)
        {
            await tx.RollbackAsync();
            var latest = await _db.Pockets.AsNoTracking().Where(p => p.Id == sourcePocketId).Select(p => p.Balance).FirstAsync();
            return ServiceResult.Fail(InsufficientMessage(latest), nameof(input.Amount));
        }

        var credited = await _db.Pockets
            .Where(p => p.Id == destinationId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance + amount));

        if (credited == 0)
        {
            await tx.RollbackAsync();
            return ServiceResult.NotFound();
        }

        _db.Transactions.Add(new Transaction
        {
            PocketId = sourcePocketId,
            Type = TransactionType.TransferOut,
            Amount = amount,
            Title = $"Transfer ke {destination.Name}",
            Note = note,
            Date = date,
            CreatedAt = now,
            CreatedByUserId = userId,
            RelatedPocketId = destination.Id,
            RelatedPocketName = destination.Name,
            TransferGroupId = groupId
        });

        _db.Transactions.Add(new Transaction
        {
            PocketId = destinationId,
            Type = TransactionType.TransferIn,
            Amount = amount,
            Title = $"Transfer dari {source.Name}",
            Note = note,
            Date = date,
            CreatedAt = now,
            CreatedByUserId = userId,
            RelatedPocketId = source.Id,
            RelatedPocketName = source.Name,
            TransferGroupId = groupId
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> EditTransactionAsync(string userId, int pocketId, TransactionEditInput input)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == input.TransactionId && t.PocketId == pocketId);
        if (tx is null)
        {
            return ServiceResult.NotFound();
        }

        if (tx.Type is TransactionType.TransferIn or TransactionType.TransferOut)
        {
            return ServiceResult.Fail("Transaksi transfer tidak dapat diedit. Batalkan transfer lalu buat ulang jika perlu.");
        }

        var allowed = role == PocketRole.Owner ||
            (role == PocketRole.Spender && tx.Type == TransactionType.Expense && tx.CreatedByUserId == userId);
        if (!allowed)
        {
            return ServiceResult.Forbidden("Anda tidak memiliki izin mengubah transaksi ini.");
        }

        var newAmount = NormalizeAmount(input.Amount);
        if (newAmount <= 0)
        {
            return ServiceResult.Fail("Nominal harus lebih besar dari Rp 0.", nameof(input.Amount));
        }

        var title = (input.Title ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            return ServiceResult.Fail("Keterangan wajib diisi.", nameof(input.Title));
        }

        var dateError = ValidateDate(input.Date, nameof(input.Date), out var date);
        if (dateError is not null)
        {
            return dateError;
        }

        int? resolvedCategoryId = tx.CategoryId;
        string? resolvedCategoryName = tx.CategoryName;
        string? resolvedGroupName = tx.GroupName;

        if (tx.Type == TransactionType.Expense)
        {
            if (input.CategoryId is null)
            {
                return ServiceResult.Fail("Pilih alokasi pengeluaran.", nameof(input.CategoryId));
            }

            // Jika kategori tidak diubah (misal Owner mengedit transaksi milik Spender lain), pertahankan snapshot lama.
            if (input.CategoryId != tx.CategoryId)
            {
                var cat = await _db.ExpenseCategories.AsNoTracking()
                    .Where(c => c.Id == input.CategoryId && c.Group.UserId == userId)
                    .Select(c => new { c.Id, c.Name, GroupName = c.Group.Name })
                    .FirstOrDefaultAsync();

                if (cat is null)
                {
                    return ServiceResult.Fail("Alokasi pengeluaran tidak valid.", nameof(input.CategoryId));
                }

                resolvedCategoryId = cat.Id;
                resolvedCategoryName = cat.Name;
                resolvedGroupName = cat.GroupName;
            }
        }

        var oldAmount = tx.Amount;
        var delta = newAmount - oldAmount;

        await using var dbTx = await _db.Database.BeginTransactionAsync();

        if (delta != 0)
        {
            if (tx.Type == TransactionType.Expense)
            {
                if (delta > 0)
                {
                    var debited = await _db.Pockets
                        .Where(p => p.Id == pocketId && p.Balance >= delta)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance - delta));

                    if (debited == 0)
                    {
                        await dbTx.RollbackAsync();
                        var latest = await _db.Pockets.AsNoTracking().Where(p => p.Id == pocketId).Select(p => p.Balance).FirstAsync();
                        return ServiceResult.Fail($"Nominal melebihi batas saldo tersedia ({(latest + oldAmount).ToRupiah()}).", nameof(input.Amount));
                    }
                }
                else
                {
                    var refund = -delta;
                    await _db.Pockets
                        .Where(p => p.Id == pocketId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance + refund));
                }
            }
            else if (tx.Type == TransactionType.Income)
            {
                if (delta > 0)
                {
                    await _db.Pockets
                        .Where(p => p.Id == pocketId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance + delta));
                }
                else
                {
                    var reduction = -delta;
                    var deducted = await _db.Pockets
                        .Where(p => p.Id == pocketId && p.Balance >= reduction)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance - reduction));

                    if (deducted == 0)
                    {
                        await dbTx.RollbackAsync();
                        return ServiceResult.Fail("Tidak dapat memperkecil nominal pemasukan karena sebagian dana sudah digunakan.", nameof(input.Amount));
                    }
                }
            }
        }

        tx.Amount = newAmount;
        tx.Title = title;
        if (tx.Type == TransactionType.Income)
        {
            tx.Source = title;
        }
        else
        {
            tx.CategoryId = resolvedCategoryId;
            tx.CategoryName = resolvedCategoryName;
            tx.GroupName = resolvedGroupName;
        }
        tx.Date = date;
        tx.Note = CleanOptional(input.Note);

        await _db.SaveChangesAsync();
        await dbTx.CommitAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteTransactionAsync(string userId, int pocketId, long transactionId)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.PocketId == pocketId);
        if (tx is null)
        {
            return ServiceResult.NotFound();
        }

        if (tx.Type is TransactionType.TransferIn or TransactionType.TransferOut)
        {
            if (role != PocketRole.Owner)
            {
                return ServiceResult.Forbidden("Hanya Owner yang dapat membatalkan transaksi transfer.");
            }

            if (tx.TransferGroupId is null)
            {
                return ServiceResult.Fail("Data pasangan transfer tidak ditemukan.");
            }

            var pair = await _db.Transactions
                .Where(t => t.TransferGroupId == tx.TransferGroupId)
                .ToListAsync();

            var outTx = pair.FirstOrDefault(t => t.Type == TransactionType.TransferOut);
            var inTx = pair.FirstOrDefault(t => t.Type == TransactionType.TransferIn);

            if (outTx is null || inTx is null || pair.Count != 2)
            {
                return ServiceResult.Fail("Transfer ini terhubung ke kantong yang sudah dihapus sehingga tidak dapat dibatalkan.");
            }

            var sourceRole = await GetRoleAsync(userId, outTx.PocketId);
            var destRole = await GetRoleAsync(userId, inTx.PocketId);
            if (sourceRole != PocketRole.Owner || destRole != PocketRole.Owner)
            {
                return ServiceResult.Forbidden("Anda harus menjadi Owner pada kedua kantong untuk membatalkan transfer ini.");
            }

            await using var transferDbTx = await _db.Database.BeginTransactionAsync();

            var deducted = await _db.Pockets
                .Where(p => p.Id == inTx.PocketId && p.Balance >= inTx.Amount)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance - inTx.Amount));

            if (deducted == 0)
            {
                await transferDbTx.RollbackAsync();
                return ServiceResult.Fail("Transfer tidak dapat dibatalkan karena saldo di kantong tujuan sudah terpakai.");
            }

            await _db.Pockets
                .Where(p => p.Id == outTx.PocketId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance + outTx.Amount));

            _db.Transactions.RemoveRange(pair);
            await _db.SaveChangesAsync();
            await transferDbTx.CommitAsync();
            return ServiceResult.Ok();
        }

        var canDelete = role == PocketRole.Owner ||
            (role == PocketRole.Spender && tx.Type == TransactionType.Expense && tx.CreatedByUserId == userId);
        if (!canDelete)
        {
            return ServiceResult.Forbidden("Anda tidak memiliki izin menghapus transaksi ini.");
        }

        await using var dbTx = await _db.Database.BeginTransactionAsync();

        var deleted = await _db.Transactions
            .Where(t => t.Id == transactionId && t.PocketId == pocketId)
            .ExecuteDeleteAsync();

        if (deleted == 0)
        {
            await dbTx.RollbackAsync();
            return ServiceResult.NotFound();
        }

        if (tx.Type == TransactionType.Expense)
        {
            await _db.Pockets
                .Where(p => p.Id == pocketId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance + tx.Amount));
        }
        else if (tx.Type == TransactionType.Income)
        {
            var deducted = await _db.Pockets
                .Where(p => p.Id == pocketId && p.Balance >= tx.Amount)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Balance, p => p.Balance - tx.Amount));

            if (deducted == 0)
            {
                await dbTx.RollbackAsync();
                return ServiceResult.Fail("Pemasukan tidak dapat dihapus karena sebagian dananya sudah terpakai.");
            }
        }

        await dbTx.CommitAsync();
        return ServiceResult.Ok();
    }

    // =====================================================================
    // Shared pocket
    // =====================================================================

    public async Task<ServiceResult> InviteMemberAsync(string userId, int pocketId, InviteInput input)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        if (role != PocketRole.Owner)
        {
            return ServiceResult.Forbidden("Hanya Owner yang dapat mengundang anggota.");
        }

        if (input.Role is not (PocketRole.Spender or PocketRole.Viewer))
        {
            return ServiceResult.Fail("Hak akses undangan harus Spender atau Viewer.", nameof(input.Role));
        }

        var email = (input.Email ?? string.Empty).Trim();
        if (email.Length == 0)
        {
            return ServiceResult.Fail("Email anggota wajib diisi.", nameof(input.Email));
        }

        var normalized = email.ToUpperInvariant();
        var invitee = await _db.Users.AsNoTracking()
            .Where(u => u.NormalizedEmail == normalized)
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .FirstOrDefaultAsync();

        if (invitee is null)
        {
            return ServiceResult.Fail("Email tersebut belum terdaftar di Kantongku. Minta teman Anda mendaftar terlebih dahulu.", nameof(input.Email));
        }

        if (invitee.Id == userId)
        {
            return ServiceResult.Fail("Anda sudah menjadi Owner kantong ini.", nameof(input.Email));
        }

        var already = await _db.PocketMembers.AnyAsync(m => m.PocketId == pocketId && m.UserId == invitee.Id);
        if (already)
        {
            return ServiceResult.Fail("Pengguna tersebut sudah menjadi anggota kantong ini.", nameof(input.Email));
        }

        _db.PocketMembers.Add(new PocketMember
        {
            PocketId = pocketId,
            UserId = invitee.Id,
            Role = input.Role,
            JoinedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ChangeMemberRoleAsync(string userId, int pocketId, int memberId, PocketRole newRole)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        if (role != PocketRole.Owner)
        {
            return ServiceResult.Forbidden("Hanya Owner yang dapat mengubah hak akses anggota.");
        }

        if (newRole is not (PocketRole.Spender or PocketRole.Viewer))
        {
            return ServiceResult.Fail("Hak akses harus Spender atau Viewer.");
        }

        var member = await _db.PocketMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.PocketId == pocketId);
        if (member is null)
        {
            return ServiceResult.NotFound();
        }

        if (member.Role == PocketRole.Owner)
        {
            return ServiceResult.Fail("Hak akses Owner tidak dapat diubah.");
        }

        member.Role = newRole;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveMemberAsync(string userId, int pocketId, int memberId)
    {
        var role = await GetRoleAsync(userId, pocketId);
        if (role is null)
        {
            return ServiceResult.NotFound();
        }

        if (role != PocketRole.Owner)
        {
            return ServiceResult.Forbidden("Hanya Owner yang dapat menghapus anggota.");
        }

        var member = await _db.PocketMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.PocketId == pocketId);
        if (member is null)
        {
            return ServiceResult.NotFound();
        }

        if (member.Role == PocketRole.Owner)
        {
            return ServiceResult.Fail("Owner tidak dapat dihapus dari kantongnya sendiri.");
        }

        _db.PocketMembers.Remove(member);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> LeavePocketAsync(string userId, int pocketId)
    {
        var member = await _db.PocketMembers.FirstOrDefaultAsync(m => m.PocketId == pocketId && m.UserId == userId);
        if (member is null)
        {
            return ServiceResult.NotFound();
        }

        if (member.Role == PocketRole.Owner)
        {
            return ServiceResult.Fail("Owner tidak dapat keluar dari kantong. Hapus kantong jika tidak diperlukan lagi.");
        }

        _db.PocketMembers.Remove(member);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
