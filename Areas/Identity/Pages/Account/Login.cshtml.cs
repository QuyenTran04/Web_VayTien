
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using VAYTIEN.Models;

namespace VAYTIEN.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly EmailService _emailService;
        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager, EmailService emailService,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Số điện thoại hoặc Email")]
            public string Username { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ghi nhớ đăng nhập")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }
        private string GenerateOtp()
        {
            var rnd = new Random();
            return rnd.Next(100000, 999999).ToString();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                ApplicationUser user = null;

                if (Input.Username.Contains("@"))
                {
                    user = await _userManager.FindByEmailAsync(Input.Username);
                }
                else
                {
                    user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == Input.Username);
                }

                if (user != null)
                {
                    var result = await _signInManager.CheckPasswordSignInAsync(user, Input.Password, false);
                    if (result.Succeeded)
                    {
                        var roles = await _userManager.GetRolesAsync(user);

                        if (roles.Contains(SD.Role_Admin)) // Chỉ gửi OTP nếu là admin
                        {
                            // Sinh OTP
                            var otp = GenerateOtp();
                            HttpContext.Session.SetString("Otp", otp);
                            HttpContext.Session.SetString("OtpUserId", user.Id);
                            HttpContext.Session.SetString("OtpExpired", DateTime.UtcNow.AddMinutes(2).ToString());

                            await _emailService.SendAsync(
                                user.Email,
                                "Mã xác thực đăng nhập",
                                $"Mã OTP của bạn là: <b>{otp}</b> (có hiệu lực trong 2 phút)");

                            return RedirectToPage("./VerifyOtp", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                        }
                        else // Nếu không phải admin thì đăng nhập thẳng
                        {
                            await _signInManager.SignInAsync(user, Input.RememberMe);
                            return RedirectToAction("Index", "Home", new { area = "" });
                        }
                    }

                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }

                    ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không đúng.");
                }

                ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không đúng.");
            }

            return Page();
        }
    }
}