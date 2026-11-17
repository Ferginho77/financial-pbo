using System.Globalization;
using System.Text;
using Kantongku.Helpers;
using Kantongku.Models;
using Kantongku.Models.ViewModels;
using Kantongku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantongku.Controllers;

[Authorize]
public class PocketsController : Controller
{
    private readonly IPocketService _pockets;

    public PocketsController(IPocketService pockets)
    {
        _pockets = pockets;
    }

    private string UserId => User.GetUserId();

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Home");

    // ------------------------------------------------------------------
    // Detail & Ekspor CSV
    // ------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? filter, string? q = null, string? month = null, int page = 1, string? open = null)
    {
        var vm = await _pockets.GetDetailsAsync(UserId, id, filter, q, month, page);
        if (vm is null)
        {
            return NotFound();
        }

        vm.OpenDialog = open switch
        {
            "income" when vm.CanManage => "income",
            "expense" when vm.CanSpend => "expense",
            "transfer" when vm.CanManage => "transfer",
            _ => null
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(int id, string? filter, string? q = null, string? month = null)
    {
        var export = await _pockets.GetTransactionsForExportAsync(UserId, id, filter, q, month);
        if (export is null)
        {
            return NotFound();
        }

        var (pocketName, rows) = export.Value;
        var sb = new StringBuilder();
        sb.AppendLine("Tanggal;Jenis;Keterangan;Kategori;Catatan;Dicatat Oleh;Masuk (IDR);Keluar (IDR)");

        foreach (var r in rows)
        {
            var credit = r.IsCredit ? r.Amount.ToString("0", CultureInfo.InvariantCulture) : "0";
            var debit = r.IsCredit ? "0" : r.Amount.ToString("0", CultureInfo.InvariantCulture);
            sb.Append(CsvCell(r.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Append(';')
              .Append(CsvCell(r.Type.Label())).Append(';')
              .Append(CsvCell(r.Title)).Append(';')
              .Append(CsvCell(r.CategoryLabel ?? "-")).Append(';')
              .Append(CsvCell(r.Note ?? string.Empty)).Append(';')
              .Append(CsvCell(r.ActorLabel)).Append(';')
              .Append(credit).Append(';')
              .Append(debit).AppendLine();
        }

        var bom = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, bytes, 0, bom.Length);
        Buffer.BlockCopy(body, 0, bytes, bom.Length, body.Length);

        var slug = new string(pocketName.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        if (string.IsNullOrEmpty(slug))
        {
            slug = "kantong";
        }

        var fileName = $"mutasi-{slug}-{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    // ------------------------------------------------------------------
    // Create / Edit / Delete Pocket
    // ------------------------------------------------------------------

    [HttpGet]
    public IActionResult Create() => View(new PocketFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PocketFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _pockets.CreatePocketAsync(UserId, model);
        if (result.Succeeded)
        {
            TempData["Success"] = $"Kantong \"{model.Name.Trim()}\" berhasil dibuat.";
            return RedirectToAction(nameof(Details), new { id = result.EntityId });
        }

        ModelState.AddModelError(result.Field ?? string.Empty, result.Error ?? "Gagal membuat kantong.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var role = await _pockets.GetRoleAsync(UserId, id);
        if (role is null)
        {
            return NotFound();
        }

        if (role != PocketRole.Owner)
        {
            return Forbid();
        }

        var model = await _pockets.GetForEditAsync(UserId, id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PocketFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _pockets.UpdatePocketAsync(UserId, model);
        if (result.Succeeded)
        {
            TempData["Success"] = "Perubahan kantong disimpan.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var blocked = BlockedResult(result);
        if (blocked is not null)
        {
            return blocked;
        }

        ModelState.AddModelError(result.Field ?? string.Empty, result.Error ?? "Gagal menyimpan perubahan.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _pockets.DeletePocketAsync(UserId, id);
        if (result.Succeeded)
        {
            TempData["Success"] = "Kantong berhasil dihapus.";
            return RedirectToAction("Index", "Home");
        }

        return BlockedResult(result) ?? FlashAndRedirect(result, id);
    }

    // ------------------------------------------------------------------
    // Transaksi
    // ------------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddIncome(int id, [Bind(Prefix = nameof(PocketDetailsViewModel.Income))] IncomeInput input)
    {
        var guard = await RequireRoleAsync(id, PocketRole.Owner);
        if (guard is not null)
        {
            return guard;
        }

        if (ModelState.IsValid)
        {
            var result = await _pockets.AddIncomeAsync(UserId, id, input);
            if (result.Succeeded)
            {
                TempData["Success"] = "Pemasukan berhasil dicatat.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var blocked = BlockedResult(result);
            if (blocked is not null)
            {
                return blocked;
            }

            AddError(nameof(PocketDetailsViewModel.Income), result);
        }

        return await RenderDetailsAsync(id, "income", vm => vm.Income = input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense(int id, [Bind(Prefix = nameof(PocketDetailsViewModel.Expense))] ExpenseInput input)
    {
        var guard = await RequireRoleAsync(id, PocketRole.Spender);
        if (guard is not null)
        {
            return guard;
        }

        if (ModelState.IsValid)
        {
            var result = await _pockets.AddExpenseAsync(UserId, id, input);
            if (result.Succeeded)
            {
                TempData["Success"] = "Pengeluaran berhasil dicatat dan saldo kantong dikurangi.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var blocked = BlockedResult(result);
            if (blocked is not null)
            {
                return blocked;
            }

            AddError(nameof(PocketDetailsViewModel.Expense), result);
        }

        return await RenderDetailsAsync(id, "expense", vm => vm.Expense = input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(int id, [Bind(Prefix = nameof(PocketDetailsViewModel.Transfer))] TransferInput input)
    {
        var guard = await RequireRoleAsync(id, PocketRole.Owner);
        if (guard is not null)
        {
            return guard;
        }

        if (ModelState.IsValid)
        {
            var result = await _pockets.TransferAsync(UserId, id, input);
            if (result.Succeeded)
            {
                TempData["Success"] = "Transfer antar-kantong berhasil.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var blocked = BlockedResult(result);
            if (blocked is not null)
            {
                return blocked;
            }

            AddError(nameof(PocketDetailsViewModel.Transfer), result);
        }

        return await RenderDetailsAsync(id, "transfer", vm => vm.Transfer = input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTransaction(
        int id,
        [Bind(Prefix = nameof(PocketDetailsViewModel.EditTransaction))] TransactionEditInput input,
        string? filter,
        string? q,
        string? month,
        int page = 1)
    {
        var guard = await RequireRoleAsync(id, PocketRole.Spender);
        if (guard is not null)
        {
            return guard;
        }

        if (ModelState.IsValid)
        {
            var result = await _pockets.EditTransactionAsync(UserId, id, input);
            if (result.Succeeded)
            {
                TempData["Success"] = "Transaksi diperbarui dan saldo disesuaikan.";
                return RedirectToAction(nameof(Details), new { id, filter, q, month, page });
            }

            var blocked = BlockedResult(result);
            if (blocked is not null)
            {
                return blocked;
            }

            AddError(nameof(PocketDetailsViewModel.EditTransaction), result);
        }

        return await RenderDetailsAsync(id, "editTx", vm => vm.EditTransaction = input, filter, q, month, page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTransaction(int id, long transactionId, string? filter, string? q, string? month, int page = 1)
    {
        var guard = await RequireRoleAsync(id, PocketRole.Spender);
        if (guard is not null)
        {
            return guard;
        }

        var result = await _pockets.DeleteTransactionAsync(UserId, id, transactionId);
        if (result.Succeeded)
        {
            TempData["Success"] = "Transaksi dibatalkan dan saldo disesuaikan.";
            return RedirectToAction(nameof(Details), new { id, filter, q, month, page });
        }

        var blocked = BlockedResult(result);
        if (blocked is not null)
        {
            return blocked;
        }

        TempData["Error"] = result.Error ?? "Gagal menghapus transaksi.";
        return RedirectToAction(nameof(Details), new { id, filter, q, month, page });
    }

    // ------------------------------------------------------------------
    // Anggota Shared Pocket
    // ------------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(int id, [Bind(Prefix = nameof(PocketDetailsViewModel.Invite))] InviteInput input)
    {
        var guard = await RequireRoleAsync(id, PocketRole.Owner);
        if (guard is not null)
        {
            return guard;
        }

        if (ModelState.IsValid)
        {
            var result = await _pockets.InviteMemberAsync(UserId, id, input);
            if (result.Succeeded)
            {
                TempData["Success"] = $"{input.Email.Trim()} berhasil ditambahkan sebagai {input.Role.Label()}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var blocked = BlockedResult(result);
            if (blocked is not null)
            {
                return blocked;
            }

            AddError(nameof(PocketDetailsViewModel.Invite), result);
        }

        return await RenderDetailsAsync(id, "invite", vm => vm.Invite = input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(int id, int memberId, PocketRole role)
    {
        var result = await _pockets.ChangeMemberRoleAsync(UserId, id, memberId, role);
        if (result.Succeeded)
        {
            TempData["Success"] = "Hak akses anggota diperbarui.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return BlockedResult(result) ?? FlashAndRedirect(result, id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, int memberId)
    {
        var result = await _pockets.RemoveMemberAsync(UserId, id, memberId);
        if (result.Succeeded)
        {
            TempData["Success"] = "Anggota dihapus dari kantong.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return BlockedResult(result) ?? FlashAndRedirect(result, id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var result = await _pockets.LeavePocketAsync(UserId, id);
        if (result.Succeeded)
        {
            TempData["Success"] = "Anda telah keluar dari kantong bersama.";
            return RedirectToAction("Index", "Home");
        }

        return BlockedResult(result) ?? FlashAndRedirect(result, id);
    }

    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------

    private async Task<IActionResult?> RequireRoleAsync(int pocketId, PocketRole minimum)
    {
        var role = await _pockets.GetRoleAsync(UserId, pocketId);
        if (role is null)
        {
            return NotFound();
        }

        return role.Value >= minimum ? null : Forbid();
    }

    private IActionResult? BlockedResult(ServiceResult result) => result.Kind switch
    {
        ResultKind.NotFound => NotFound(),
        ResultKind.Forbidden => Forbid(),
        _ => null
    };

    private IActionResult FlashAndRedirect(ServiceResult result, int id)
    {
        TempData["Error"] = result.Error ?? "Terjadi kesalahan.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private void AddError(string prefix, ServiceResult result)
    {
        var key = string.IsNullOrEmpty(result.Field) ? string.Empty : $"{prefix}.{result.Field}";
        ModelState.AddModelError(key, result.Error ?? "Terjadi kesalahan.");
    }

    private async Task<IActionResult> RenderDetailsAsync(
        int id,
        string? dialog,
        Action<PocketDetailsViewModel> apply,
        string? filter = null,
        string? q = null,
        string? month = null,
        int page = 1)
    {
        var vm = await _pockets.GetDetailsAsync(UserId, id, filter, q, month, page);
        if (vm is null)
        {
            return NotFound();
        }

        apply(vm);
        vm.OpenDialog = dialog;
        return View("Details", vm);
    }

    private static string CsvCell(string value)
    {
        var sanitized = value.Replace("\r", " ").Replace("\n", " ");
        if (sanitized.Length > 0 && sanitized[0] is '=' or '+' or '-' or '@' or '\t')
        {
            sanitized = "'" + sanitized;
        }

        return "\"" + sanitized.Replace("\"", "\"\"") + "\"";
    }
}
