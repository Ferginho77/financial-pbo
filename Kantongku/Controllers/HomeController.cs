using System.Diagnostics;
using Kantongku.Helpers;
using Kantongku.Models.ViewModels;
using Kantongku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantongku.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IPocketService _pockets;

    public HomeController(IPocketService pockets)
    {
        _pockets = pockets;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? month = null)
    {
        var model = await _pockets.GetDashboardAsync(User.GetUserId(), month);
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Status(int id)
    {
        var model = id switch
        {
            404 => new ErrorViewModel { Code = 404, Title = "Halaman tidak ditemukan", Message = "Halaman atau data yang Anda cari tidak ada, atau Anda tidak memiliki akses ke sana." },
            403 => new ErrorViewModel { Code = 403, Title = "Akses ditolak", Message = "Anda tidak memiliki izin untuk melakukan tindakan ini." },
            _ => new ErrorViewModel { Code = id, Title = "Terjadi kesalahan", Message = "Permintaan Anda tidak dapat diproses." }
        };

        model.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        Response.StatusCode = id;
        return View("~/Views/Shared/Error.cshtml", model);
    }

    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var model = new ErrorViewModel
        {
            Code = 500,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };
        Response.StatusCode = 500;
        return View("~/Views/Shared/Error.cshtml", model);
    }
}
