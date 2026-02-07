using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ClinicMIS.Models.Entities;
using ClinicMIS.Models.ViewModels;
using ClinicMIS.Data;

namespace ClinicMIS.Controllers;

/// <summary>
/// Account management controller
/// Handles login, logout, and password management
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ClinicDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ClinicDbContext context,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET: Account/Login
    /// </summary>
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// POST: Account/Login
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // Check if user is active
        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Your account has been deactivated. Please contact administrator.");
            return View(model);
        }

        // Check lockout
        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            ModelState.AddModelError("", $"Account locked. Try again after {user.LockedUntil.Value.ToLocalTime():HH:mm}");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} logged in.", model.Email);

            // Log audit
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.Email,
                Action = AuditAction.Login,
                EntityName = "ApplicationUser",
                EntityId = user.Id,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} locked out.", model.Email);
            ModelState.AddModelError("", "Account locked due to multiple failed attempts. Try again later.");
            return View(model);
        }

        // Increment failed attempts
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= 5)
        {
            user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
        }
        await _userManager.UpdateAsync(user);

        ModelState.AddModelError("", "Invalid login attempt.");
        return View(model);
    }

    /// <summary>
    /// POST: Account/Logout
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        
        // Log audit
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserName = User.Identity?.Name,
            Action = AuditAction.Logout,
            EntityName = "ApplicationUser",
            EntityId = userId,
            Timestamp = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _context.SaveChangesAsync();

        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {Name} logged out.", User.Identity?.Name);

        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// GET: Account/ChangePassword
    /// </summary>
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    /// <summary>
    /// POST: Account/ChangePassword
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            // Log audit
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.Email,
                Action = AuditAction.PasswordChange,
                EntityName = "ApplicationUser",
                EntityId = user.Id,
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }

    /// <summary>
    /// GET: Account/AccessDenied
    /// </summary>
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
