using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using VAYTIEN.Models;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Customer")]
public class ChatController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly QlvayTienContext _context;

    public ChatController(UserManager<ApplicationUser> userManager, QlvayTienContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> UserChat()
    {
        // Lấy admin đầu tiên
        var admin = (await _userManager.GetUsersInRoleAsync("Admin")).FirstOrDefault();
        ViewBag.AdminId = admin?.Id;
        ViewBag.SelfId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        ViewBag.SelfName = User.Identity.Name;

        // Lấy lịch sử chat riêng
        var userId = ViewBag.SelfId as string;
        var adminId = ViewBag.AdminId as string;
        var messages = await _context.ChatMessages
            .Where(m => (m.SenderId == userId && m.ReceiverId == adminId)
                     || (m.SenderId == adminId && m.ReceiverId == userId))
            .OrderBy(m => m.SentAt)
            .ToListAsync();
        ViewBag.ChatHistory = messages;
        return View();
    }

}
