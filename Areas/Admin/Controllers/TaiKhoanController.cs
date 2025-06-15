using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAYTIEN.Models;
using System.Linq;
using System.Threading.Tasks;

namespace VAYTIEN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class TaiKhoanController : Controller
    {
        private readonly QlvayTienContext _context;

        public TaiKhoanController(QlvayTienContext context)
        {
            _context = context;
        }

        // GET: Admin/TaiKhoan
        // Hiển thị trang danh sách tất cả tài khoản của khách hàng
        public async Task<IActionResult> Index()
        {
            var customerAccounts = await _context.TaiKhoanNganHangs
                                        .Where(tk => tk.MaKh != null) // Chỉ lấy tài khoản của khách hàng
                                        .Include(tk => tk.MaKhNavigation) // Lấy kèm thông tin khách hàng
                                        .OrderBy(tk => tk.MaKhNavigation.HoTen)
                                        .ToListAsync();

            return View(customerAccounts);
        }
    }
}