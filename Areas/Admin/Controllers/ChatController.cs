using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VAYTIEN.Models;
using Microsoft.EntityFrameworkCore;

namespace VAYTIEN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChatController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QlvayTienContext _context;

        public ChatController(UserManager<ApplicationUser> userManager, QlvayTienContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> AdminChat(string userId)
        {
            var users = await _userManager.GetUsersInRoleAsync("Customer");
            ViewBag.Users = users;
            ViewBag.CurrentUserId = userId;
            ViewBag.SelfId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ViewBag.SelfName = User.Identity.Name;

            // Ép kiểu về string để dùng trong LINQ
            var adminId = ViewBag.SelfId as string;

            // Lịch sử chat riêng giữa admin và user được chọn
            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == adminId)
                         || (m.SenderId == adminId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.ChatHistory = messages;
            return View();
        }
    }
}
