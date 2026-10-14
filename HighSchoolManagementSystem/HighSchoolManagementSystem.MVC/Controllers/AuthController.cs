using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Auth;
using HighSchoolManagementSystem.Services.Interfaces;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.MVC.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(IAuthService authService, AppDbContext db, IPasswordHasher passwordHasher)
    {
        _authService = authService;
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true
        ? RedirectToDashboardByRole(User.FindFirstValue(ClaimTypes.Role))
        : View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDashboardByRole(User.FindFirstValue(ClaimTypes.Role));

        var username = model.Username.Trim();
        var email = model.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Username == username))
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã được sử dụng.");
        if (await _db.Users.AnyAsync(u => u.Email == email))
            ModelState.AddModelError(nameof(model.Email), "Email đã được đăng ký.");
        if (!ModelState.IsValid) return View(model);

        var studentRole = await _db.Roles.SingleAsync(r => r.RoleName == "Student");
        var user = new User
        {
            Username = username, Email = email, FullName = model.FullName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(model.Password), RoleId = studentRole.RoleId,
            IsActive = true, CreateAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _db.Students.Add(new Student
        {
            UserId = user.Id,
            StudentCode = $"AMS{DateTime.UtcNow:yyMMdd}{Random.Shared.Next(1000, 9999)}",
            AdmissionDate = DateTime.UtcNow.Date,
            Status = "Active"
        });
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Tạo tài khoản học viên thành công. Bạn có thể đăng nhập và đăng ký lớp.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectToDashboardByRole(role);
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

        var result = await _authService.AuthenticateAsync(model.UsernameOrEmail, model.Password);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return View(model);
        }

        var user = result.Data;
        var roleName = user.Role?.RoleName ?? string.Empty;

        // Thiết lập phiên đăng nhập Cookie
        await SignInUserAsync(user, roleName, model.RememberMe);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToDashboardByRole(roleName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // =========================================================================
    // QUÊN & ĐẶT LẠI MẬT KHẨU (FORGOT & RESET PASSWORD)
    // =========================================================================

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectToDashboardByRole(role);
        }

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resetLinkBaseUrl = Url.Action("ResetPassword", "Auth", null, Request.Scheme) 
            ?? $"{Request.Scheme}://{Request.Host}/Auth/ResetPassword";

        var result = await _authService.SendResetPasswordEmailAsync(model.Email, resetLinkBaseUrl);

        if (!result.Success)
        {
            if (result.ErrorCode == "MAIL_002")
            {
                ViewBag.WarningMessage = "Hệ thống email chưa được cấu hình tài khoản gửi trong appsettings.json. Đường link đặt lại mật khẩu đã được in ra Console để hỗ trợ kiểm thử.";
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        ViewBag.SuccessMessage = "Nếu địa chỉ email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi đến hòm thư của bạn. Vui lòng kiểm tra hộp thư đến (hoặc hòm thư rác/spam).";
        ViewBag.SubmittedEmail = model.Email;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string? email, string? token)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return RedirectToDashboardByRole(role);
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc thiếu thông tin xác thực.";
            return RedirectToAction("Login");
        }

        var validateResult = await _authService.ValidatePasswordResetTokenAsync(email, token);
        if (!validateResult.Success)
        {
            TempData["ErrorMessage"] = validateResult.Message ?? "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction("Login");
        }

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Đặt lại mật khẩu thất bại.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction("Login");
    }

    // PRIVATE HELPERS

    private async Task SignInUserAsync(User user, string roleName, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe 
                ? DateTimeOffset.UtcNow.AddDays(14) 
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    private IActionResult RedirectToDashboardByRole(string? roleName)
    {
        return roleName switch
        {
            "Admin" => RedirectToAction("Dashboard", "Staff"),
            "Expert" => RedirectToAction("Templates", "Assignments"),
            "Guest" => RedirectToAction("Index", "Home"),
            "Principal" => RedirectToAction("Dashboard", "Principal"),
            "Staff" => RedirectToAction("Dashboard", "Staff"),
            "Teacher" => RedirectToAction("Dashboard", "Teacher"),
            "Student" => RedirectToAction("Dashboard", "Student"),
            "Parent" => RedirectToAction("Dashboard", "Parent"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
