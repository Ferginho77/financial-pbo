using System.Security.Claims;
using Kantongku.Helpers;
using Kantongku.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Kantongku.Services;

/// <summary>Menambahkan klaim DisplayName ke cookie login agar nama tampil di layout tanpa query tambahan.</summary>
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public AppClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options) : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(ClaimsPrincipalExtensions.DisplayNameClaim,
            string.IsNullOrWhiteSpace(user.DisplayName) ? (user.Email ?? user.UserName ?? "Pengguna") : user.DisplayName));
        return identity;
    }
}

/// <summary>Pesan error Identity dalam Bahasa Indonesia.</summary>
public class IndonesianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = $"Email '{email}' sudah terdaftar." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = $"Email '{userName}' sudah terdaftar." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "Format email tidak valid." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "Email tidak valid." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Kata sandi minimal {length} karakter." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Kata sandi harus mengandung minimal satu angka." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Kata sandi harus mengandung huruf kecil." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "Kata sandi harus mengandung huruf besar." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Kata sandi harus mengandung simbol." };

    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Terjadi kesalahan. Silakan coba lagi." };
}
