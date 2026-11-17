using Kantongku.Helpers;
using Kantongku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantongku.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories)
    {
        _categories = categories;
    }

    private string UserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var groups = await _categories.GetGroupsAsync(UserId);
        return View(groups);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(string name)
    {
        var result = await _categories.CreateGroupAsync(UserId, name);
        return Respond(result, "Kelompok kategori ditambahkan.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroup(int id, string name)
    {
        var result = await _categories.RenameGroupAsync(UserId, id, name);
        return Respond(result, "Nama kelompok diperbarui.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var result = await _categories.DeleteGroupAsync(UserId, id);
        return Respond(result, "Kelompok beserta alokasinya dihapus. Riwayat transaksi tetap tersimpan.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(int groupId, string name)
    {
        var result = await _categories.CreateCategoryAsync(UserId, groupId, name);
        return Respond(result, "Alokasi pengeluaran ditambahkan.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, string name)
    {
        var result = await _categories.RenameCategoryAsync(UserId, id, name);
        return Respond(result, "Nama alokasi diperbarui.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _categories.DeleteCategoryAsync(UserId, id);
        return Respond(result, "Alokasi dihapus. Riwayat transaksi tetap tersimpan.");
    }

    private IActionResult Respond(ServiceResult result, string successMessage)
    {
        if (result.Kind == ResultKind.Forbidden)
        {
            return Forbid();
        }

        if (result.Succeeded)
        {
            TempData["Success"] = successMessage;
        }
        else
        {
            TempData["Error"] = result.Error ?? "Terjadi kesalahan.";
        }

        return RedirectToAction(nameof(Index));
    }
}
