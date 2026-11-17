using System.ComponentModel.DataAnnotations;

namespace Kantongku.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kata sandi wajib diisi.")]
    [DataType(DataType.Password)]
    [Display(Name = "Kata sandi")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ingat saya")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter.")]
    [Display(Name = "Nama lengkap")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kata sandi wajib diisi.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Kata sandi minimal 6 karakter.")]
    [DataType(DataType.Password)]
    [Display(Name = "Kata sandi")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konfirmasi kata sandi wajib diisi.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Konfirmasi kata sandi tidak sama.")]
    [Display(Name = "Ulangi kata sandi")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileInfoInput
{
    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter.")]
    [Display(Name = "Nama tampilan")]
    public string DisplayName { get; set; } = string.Empty;
}

public class ChangePasswordInput
{
    [Required(ErrorMessage = "Kata sandi saat ini wajib diisi.")]
    [DataType(DataType.Password)]
    [Display(Name = "Kata sandi saat ini")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kata sandi baru wajib diisi.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Kata sandi baru minimal 6 karakter.")]
    [DataType(DataType.Password)]
    [Display(Name = "Kata sandi baru")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konfirmasi kata sandi baru wajib diisi.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi kata sandi baru tidak sama.")]
    [Display(Name = "Ulangi kata sandi baru")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ProfileInfoInput Info { get; set; } = new();
    public ChangePasswordInput Password { get; set; } = new();
}
