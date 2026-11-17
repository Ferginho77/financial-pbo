using Kantongku.Helpers;
using Kantongku.Models;
using Kantongku.Models.ViewModels;
using Kantongku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kantongku.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICategoryService _categories;
    private readonly IPocketService _pockets;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICategoryService categories,
        IPocketService pockets)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _categories = categories;
        _pockets = pockets;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email atau kata sandi salah.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Terlalu banyak percobaan gagal. Akun dikunci sementara, coba lagi beberapa menit lagi."
            : "Email atau kata sandi salah.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = model.DisplayName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _categories.SeedDefaultsAsync(user.Id);
        await _pockets.CreatePocketAsync(user.Id, new PocketFormViewModel
        {
            Name = "Kantong Utama",
            Description = "Kantong untuk menampung pemasukan utama Anda.",
            Color = "navy"
        });

        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] = "Selamat datang di Kantongku! Akun Anda berhasil dibuat.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(BuildProfileVm(user));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = nameof(ProfileViewModel.Info))] ProfileInfoInput info)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var vm = BuildProfileVm(user);
            vm.Info = info;
            return View("Profile", vm);
        }

        user.DisplayName = info.DisplayName.Trim();
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var err in update.Errors)
            {
                ModelState.AddModelError($"{nameof(ProfileViewModel.Info)}.{nameof(ProfileInfoInput.DisplayName)}", err.Description);
            }

            var vm = BuildProfileVm(user);
            vm.Info = info;
            return View("Profile", vm);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Nama tampilan berhasil diperbarui.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = nameof(ProfileViewModel.Password))] ChangePasswordInput input)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View("Profile", BuildProfileVm(user));
        }

        var res = await _userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!res.Succeeded)
        {
            foreach (var err in res.Errors)
            {
                var key = err.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                    ? $"{nameof(ProfileViewModel.Password)}.{nameof(ChangePasswordInput.CurrentPassword)}"
                    : string.Empty;
                var msg = err.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                    ? "Kata sandi saat ini tidak sesuai."
                    : err.Description;
                ModelState.AddModelError(key, msg);
            }

            return View("Profile", BuildProfileVm(user));
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Kata sandi berhasil diperbarui.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }

    private static ProfileViewModel BuildProfileVm(ApplicationUser user) => new()
    {
        Email = user.Email ?? string.Empty,
        CreatedAt = user.CreatedAt.ToLocalTime(),
        Info = new ProfileInfoInput { DisplayName = user.DisplayName }
    };
}
