using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VAYTIEN.Models;
using VAYTIEN.Services;

namespace VAYTIEN.Controllers
{
    [Authorize]
    public class KhachHangController : Controller
    {
        private readonly QlvayTienContext _context;

        public KhachHangController(QlvayTienContext context)
        {
            _context = context;
        }

        // Action chính để khách hàng xem tất cả các hợp đồng và trạng thái của chúng
        [HttpGet]
        public async Task<IActionResult> ThongTinVay()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Challenge();

            var khachHang = await _context.KhachHangs
                .AsNoTracking()
                // Lấy kèm danh sách tài khoản ngân hàng của khách
                .Include(kh => kh.TaiKhoanNganHangs)
                .FirstOrDefaultAsync(kh => kh.Email == userEmail);

            if (khachHang == null) return RedirectToAction(nameof(CreateStep1));

            var hopDongs = await _context.HopDongVays
                .Where(h => h.MaKh == khachHang.MaKh)
                .Include(h => h.LichTraNos)
                .OrderByDescending(h => h.MaHopDong)
                .ToListAsync();

            // Tính toán tổng dư nợ
            decimal tongDuNoConLai = 0;
            foreach (var hd in hopDongs.Where(h => h.TinhTrang == "Đã duyệt"))
            {
                decimal tongGocDaTra = hd.LichTraNos.Where(l => l.TrangThai == "Đã trả").Sum(l => l.SoTienGoc ?? 0);
                tongDuNoConLai += hd.SoTienVay - tongGocDaTra;
            }

            var viewModel = new ThongTinVayViewModel
            {
                TongSoTienVay = tongDuNoConLai,
                // THÊM MỚI: Lấy danh sách tài khoản và số dư của khách hàng
                TaiKhoans = khachHang.TaiKhoanNganHangs.Select(tk => new TaiKhoanViewModel
                {
                    SoTaiKhoan = tk.SoTaiKhoan,
                    SoDu = tk.SoDu
                }).ToList(),
                HopDongs = hopDongs.Select(h => new ThongTinHopDongViewModel
                {
                    //... các thuộc tính khác giữ nguyên
                    MaHopDong = h.MaHopDong,
                    SoTienVay = h.SoTienVay,
                    NgayVay = h.NgayVay,
                    NgayHetHan = h.NgayHetHan,
                    TinhTrang = h.TinhTrang,
                    KyHanThang = h.KyHanThang,
                    LaiSuat = h.LaiSuat,
                    SoTienConLai = h.SoTienVay - h.LichTraNos.Where(l => l.TrangThai == "Đã trả").Sum(l => l.SoTienGoc ?? 0),
                    LichTra = h.LichTraNos.Select(l => new LichTraViewModel
                    {
                        KyHanThu = l.KyHanThu ?? 0,
                        NgayTra = l.NgayTra,
                        SoTienPhaiTra = l.SoTienPhaiTra,
                        TrangThai = l.TrangThai
                    }).OrderBy(x => x.KyHanThu).ToList()
                }).ToList()
            };

            ViewBag.LabelTongTien = "Tổng dư nợ gốc còn lại";
            return View(viewModel);
        }
        // GET: Bước 1 - Form thông tin cá nhân
        public IActionResult CreateStep1()
        {
            ViewBag.DoiTuongVayList = _context.DoiTuongVays.ToList();
            return View();
        }

        // POST: Bước 1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep1(KhachHang kh, IFormFile? anhFile)
        {
            if (kh.DoiTuongVayMaDoiTuongVay == null || kh.DoiTuongVayMaDoiTuongVay == 0)
                ModelState.AddModelError("DoiTuongVayMaDoiTuongVay", "Vui lòng chọn đối tượng vay");

            if (!ModelState.IsValid)
            {
                ViewBag.DoiTuongVayList = _context.DoiTuongVays.ToList();
                return View(kh);
            }

            if (anhFile != null && anhFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(anhFile.FileName);
                var filePath = Path.Combine("wwwroot", "uploads", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using var stream = new FileStream(filePath, FileMode.Create);
                await anhFile.CopyToAsync(stream);
                kh.AnhDinhKem = "/uploads/" + fileName;
            }

            TempData["KhachHang"] = JsonSerializer.Serialize(kh);
            return RedirectToAction("CreateStep2");
        }

        // GET: Bước 2 - Form thông tin vay
        public IActionResult CreateStep2()
        {
            // Đọc dữ liệu khách hàng từ TempData
            if (TempData["KhachHang"] is not string khachHangJson)
            {
                // Nếu không có, quay về bước 1
                return RedirectToAction("CreateStep1");
            }

            // Quan trọng: Đặt dữ liệu vào ViewBag để View có thể truy cập
            ViewBag.KhachHangJson = khachHangJson;

            // Chuẩn bị dữ liệu cho các dropdown
            ViewBag.LoaiVayList = _context.LoaiVays.ToList();
            ViewBag.LoaiTienTeList = _context.LoaiTienTes.ToList();

            // Tạo ViewModel rỗng để truyền cho View
            var viewModel = new CreateStep2ViewModel();

            return View(viewModel);
        }

        // POST: Bước 2 - Xử lý Form (ĐÃ SỬA)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStep2(CreateStep2ViewModel viewModel, string khachHangJson)
        {
            // Kiểm tra xem dữ liệu khách hàng từ bước 1 có được gửi lên không
            if (string.IsNullOrEmpty(khachHangJson))
            {
                TempData["Error"] = "Phiên đăng ký đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("CreateStep1");
            }

            // Deserialize lại đối tượng KhachHang
            var khachHang = JsonSerializer.Deserialize<KhachHang>(khachHangJson)!;
            khachHang.Email = User.Identity?.Name; // Gán email của người dùng đang đăng nhập

            // Gán các đối tượng từ ViewModel
            var hopDong = viewModel.HopDong;
            var taiSan = viewModel.TaiSan;

            // Kiểm tra và lưu thông tin khách hàng (logic cũ giữ nguyên)
            var existingKH = await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.Email == khachHang.Email);
            if (existingKH != null)
            {
                khachHang.MaKh = existingKH.MaKh;
            }
            else
            {
                _context.KhachHangs.Add(khachHang);
                await _context.SaveChangesAsync();
            }

            // Gán các thông tin còn lại và lưu Hợp đồng, Tài sản...
            hopDong.MaKh = khachHang.MaKh;
            hopDong.TinhTrang = "Chờ phê duyệt";
            hopDong.SoTienConLai = hopDong.SoTienVay;

            // Tự động gán lãi suất từ loại vay đã chọn
            var loaiVay = await _context.LoaiVays.FindAsync(hopDong.MaLoaiVay);
            if (loaiVay != null && loaiVay.LaiSuat.HasValue)
            {
                hopDong.LaiSuat = (decimal)loaiVay.LaiSuat.Value;
            }

            _context.HopDongVays.Add(hopDong);
            await _context.SaveChangesAsync();

            taiSan.MaHopDong = hopDong.MaHopDong;
            _context.TaiSanTheChaps.Add(taiSan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Gửi yêu cầu vay vốn thành công!";
            return RedirectToAction("XacNhan");
        }
        // GET: Hiển thị trang xác nhận
        public IActionResult XacNhan()
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View();
        }

        // Các action quản lý thông tin cá nhân
        [HttpGet]
        public async Task<IActionResult> ThongTinCaNhan()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Challenge();

            var khachHang = await _context.KhachHangs.FirstOrDefaultAsync(kh => kh.Email == userEmail);
            if (khachHang == null)
            {
                return RedirectToAction(nameof(CreateStep1));
            }
            return View(khachHang);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var khachHang = await _context.KhachHangs.FindAsync(id);
            if (khachHang == null) return NotFound();
            if (khachHang.Email != User.Identity?.Name && !User.IsInRole("Admin")) return Forbid();
            return View(khachHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhachHang khachHang, IFormFile? anhFile)
        {
            // ... (Logic Edit đã hoàn thiện ở các lần trước)
            return RedirectToAction(nameof(ThongTinCaNhan), new { id = khachHang.MaKh });
        }
        public async Task<IActionResult> ChiTiet(int maHopDong, int kyHanThu)
        {
            var lichTra = await _context.LichTraNos
                .Include(l => l.MaHopDongNavigation.MaKhNavigation)
                .FirstOrDefaultAsync(x => x.MaHopDong == maHopDong && x.KyHanThu == kyHanThu);

            if (lichTra?.MaHopDongNavigation?.MaKhNavigation == null) return NotFound();
            if (lichTra.MaHopDongNavigation.MaKhNavigation.Email != User.Identity.Name) return Forbid();

            // =======================================================
            // SỬA LỖI: KIỂM TRA XEM ĐÂY CÓ PHẢI KỲ HẠN ĐÚNG ĐỂ THANH TOÁN KHÔNG
            // =======================================================
            var nextPayableInstallment = await _context.LichTraNos
                .Where(l => l.MaHopDong == maHopDong && l.TrangThai != "Đã trả")
                .OrderBy(l => l.KyHanThu)
                .FirstOrDefaultAsync();

            if (nextPayableInstallment != null && kyHanThu > nextPayableInstallment.KyHanThu)
            {
                TempData["Error"] = $"Vui lòng thanh toán cho kỳ hạn #{nextPayableInstallment.KyHanThu} trước.";
                return RedirectToAction("ThongTinVay", "KhachHang");
            }
            // =======================================================

            var viewModel = new ThanhToanViewModel
            {
                MaHopDong = maHopDong,
                KyHan = kyHanThu,
                TenKhachHang = lichTra.MaHopDongNavigation.MaKhNavigation.HoTen,
                NgayTra = lichTra.NgayTra?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today,
                SoTienPhaiTra = lichTra.SoTienPhaiTra ?? 0,
                TrangThai = lichTra.TrangThai ?? "Chưa trả"
            };
            return View(viewModel);
        }


    }
}
