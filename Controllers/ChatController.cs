using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VAYTIEN.Models;

namespace VAYTIEN.Controllers
{
    //[Authorize]
    public class ChatController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UserChat()
        {
            var admin = (await _userManager.GetUsersInRoleAsync("Admin")).FirstOrDefault();
            ViewBag.AdminId = admin?.Id;
            ViewBag.SelfId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ViewBag.SelfName = User.Identity.Name;
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChat(string userId)
        {
            var users = await _userManager.GetUsersInRoleAsync("Customer");
            ViewBag.Users = users;
            ViewBag.CurrentUserId = userId;
            ViewBag.SelfId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ViewBag.SelfName = User.Identity.Name;
            return View();
        }
    }
}
