using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using VAYTIEN.Models;
using System;
using System.Threading.Tasks;

public class VerifyOtpModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public VerifyOtpModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public string OtpInput { get; set; }
    public string ReturnUrl { get; set; }
    public bool RememberMe { get; set; }

    public void OnGet(string returnUrl = null, bool rememberMe = false)
    {
        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null, bool rememberMe = false)
    {
        var otp = HttpContext.Session.GetString("Otp");
        var userId = HttpContext.Session.GetString("OtpUserId");
        var expired = DateTime.Parse(HttpContext.Session.GetString("OtpExpired") ?? DateTime.UtcNow.ToString());

        if (OtpInput == otp && DateTime.UtcNow <= expired)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, rememberMe);
                // Xóa session
                HttpContext.Session.Remove("Otp");
                HttpContext.Session.Remove("OtpUserId");
                HttpContext.Session.Remove("OtpExpired");

                // Phân quyền chuyển trang
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(SD.Role_Admin))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }
        ModelState.AddModelError(string.Empty, "Mã OTP không hợp lệ hoặc đã hết hạn.");
        return Page();
    }
}
