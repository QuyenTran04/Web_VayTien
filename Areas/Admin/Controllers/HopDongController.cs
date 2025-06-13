using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAYTIEN.Models;
using VAYTIEN.Services;

namespace VAYTIEN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,NhanVien")]
    public class HopDongController : Controller
    {
        private readonly QlvayTienContext _context;
        private readonly EmailSender _emailSender;
        private readonly PdfGenerator _pdfGenerator;
        private readonly IConfiguration _configuration;

        public HopDongController(QlvayTienContext context, EmailSender emailSender, PdfGenerator pdfGenerator, IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _pdfGenerator = pdfGenerator;
            _configuration = configuration;
        }

        // GET: Admin/HopDong (Trang tổng hợp)
        public async Task<IActionResult> Index()
        {
            var list = await _context.HopDongVays.Include(h => h.MaKhNavigation).Include(h => h.MaLoaiVayNavigation).OrderByDescending(h => h.MaHopDong).ToListAsync();
            return View(list);
        }

        // GET: Admin/HopDong/ChoPheDuyet
        public async Task<IActionResult> ChoPheDuyet()
        {
            var list = await _context.HopDongVays.Include(h => h.MaKhNavigation).Where(h => h.TinhTrang == "Chờ phê duyệt").OrderByDescending(h => h.MaHopDong).ToListAsync();

            return View(list);

        }

        // POST: /Admin/HopDong/PheDuyet/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PheDuyet(int id)
        {
            var hd = await _context.HopDongVays
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaLoaiVayNavigation)
                .FirstOrDefaultAsync(h => h.MaHopDong == id);

            if (hd == null || hd.TinhTrang != "Chờ phê duyệt")
            {
                TempData["Error"] = "Hợp đồng không hợp lệ hoặc đã được xử lý.";
                return RedirectToAction(nameof(ChoPheDuyet));
            }

            // Tự động gán lãi suất nếu trong Hợp đồng chưa có
            if (hd.MaLoaiVayNavigation != null && hd.MaLoaiVayNavigation.LaiSuat.HasValue)
            {
                hd.LaiSuat = (decimal)hd.MaLoaiVayNavigation.LaiSuat.Value;
            }

            if (hd.SoTienVay <= 0 || hd.LaiSuat <= 0 || hd.KyHanThang <= 0)
            {
                TempData["Error"] = $"Lỗi: Hợp đồng #{id} thiếu thông tin quan trọng hoặc không hợp lệ (Số tiền, Lãi suất, hoặc Kỳ hạn).";
                return RedirectToAction(nameof(ChoPheDuyet));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // === BƯỚC 1: CẬP NHẬT TRẠNG THÁI HỢP ĐỒNG ===
                    hd.TinhTrang = "Đã duyệt";
                    hd.SoTienConLai = hd.SoTienVay; // Gán số nợ ban đầu
                    _context.Update(hd);

                    // === BƯỚC 2: TẠO LỊCH TRẢ NỢ CHI TIẾT ===
                    var schedule = GeneratePaymentSchedule(hd);
                    if (schedule.Any())
                    {
                        await _context.LichTraNos.AddRangeAsync(schedule);
                    }

                    // === BƯỚC 3: XỬ LÝ DÒNG TIỀN GIẢI NGÂN ===
                    var companyAccountNumber = _configuration["AppSettings:CompanyBankAccountNumber"];
                    var companyAccount = await _context.TaiKhoanNganHangs.FirstOrDefaultAsync(tk => tk.SoTaiKhoan == companyAccountNumber);

                    if (companyAccount == null)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Lỗi hệ thống: Không tìm thấy tài khoản của công ty để giải ngân.";
                        return RedirectToAction(nameof(ChoPheDuyet));
                    }

                    var customerAccount = await _context.TaiKhoanNganHangs.FirstOrDefaultAsync(tk => tk.MaKh == hd.MaKh);

                    // Tự động tạo tài khoản cho khách hàng nếu chưa có
                    if (customerAccount == null)
                    {
                        if (string.IsNullOrEmpty(hd.MaKhNavigation?.Sdt))
                        {
                            await transaction.RollbackAsync();
                            TempData["Error"] = $"Lỗi: Khách hàng '{hd.MaKhNavigation?.HoTen}' chưa có SĐT để tạo tài khoản mặc định.";
                            return RedirectToAction(nameof(ChoPheDuyet));
                        }

                        customerAccount = new TaiKhoanNganHang
                        {
                            MaKh = hd.MaKh,
                            SoTaiKhoan = hd.MaKhNavigation.Sdt,
                            LoaiTaiKhoan = "Tài khoản Thanh toán",
                            SoDu = 0,
                            TrangThai = "Hoạt động"
                        };
                        _context.TaiKhoanNganHangs.Add(customerAccount);
                    }

                    decimal loanAmount = hd.SoTienVay;
                    if (companyAccount.SoDu >= loanAmount)
                    {
                        companyAccount.SoDu -= loanAmount;
                        customerAccount.SoDu += loanAmount;

                        var disbursementTransaction = new GiaoDich
                        {
                            MaTaiKhoan = companyAccount.MaTaiKhoan,
                            NgayGd = DateOnly.FromDateTime(DateTime.Now),
                            SoTienGd = -loanAmount,
                            LoaiGd = "Giải ngân",
                            NoiDungGd = $"Giải ngân cho HĐ #{hd.MaHopDong} của KH {hd.MaKhNavigation?.HoTen}"
                        };
                        _context.GiaoDiches.Add(disbursementTransaction);
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Lỗi: Tài khoản của công ty không đủ số dư để giải ngân.";
                        return RedirectToAction(nameof(ChoPheDuyet));
                    }

                    await _context.SaveChangesAsync();

                    // === BƯỚC 4: TẠO PDF VÀ GỬI EMAIL ===
                    var pdfPath = _pdfGenerator.GenerateHopDongTinDungPdf(hd, hd.MaKhNavigation);
                    var emailBody = $@"<p>Kính gửi Quý khách <strong>{hd.MaKhNavigation.HoTen}</strong>,</p>
                                        <p>Yêu cầu vay vốn của Quý khách đã được phê duyệt và giải ngân thành công.</p>";
                    await _emailSender.SendEmailAsync(hd.MaKhNavigation.Email, "Thông báo Phê duyệt và Giải ngân Khoản vay", emailBody, pdfPath);

                    await transaction.CommitAsync();
                    TempData["Success"] = $"Hợp đồng #{id} đã được duyệt và giải ngân thành công!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(ChoPheDuyet));
        }

        // Action Từ chối
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoi(int id, string lyDo)
        {
            var hd = await _context.HopDongVays.Include(h => h.MaKhNavigation).FirstOrDefaultAsync(h => h.MaHopDong == id);
            if (hd == null || hd.TinhTrang != "Chờ phê duyệt")
            {
                TempData["Error"] = "Hợp đồng không hợp lệ.";
                return RedirectToAction(nameof(ChoPheDuyet));
            }
            hd.TinhTrang = "Đã từ chối";
            _context.Update(hd);
            await _context.SaveChangesAsync();
            var emailBody = $"<p>Kính gửi Quý khách <strong>{hd.MaKhNavigation.HoTen}</strong>,</p>" +
                            "<p>Chúng tôi rất tiếc phải thông báo yêu cầu vay của Quý khách đã không được phê duyệt.</p>" +
                            $"<p>Lý do: {lyDo}</p>";
            await _emailSender.SendEmailAsync(hd.MaKhNavigation.Email!, "Thông báo Kết quả Yêu cầu Vay vốn", emailBody);
            TempData["Success"] = $"Đã từ chối hợp đồng #{id}.";
            return RedirectToAction(nameof(ChoPheDuyet));
        }


        // Hàm riêng để tạo lịch trả nợ
        private List<LichTraNo> GeneratePaymentSchedule(HopDongVay hopDong)
        {
            var schedule = new List<LichTraNo>();

            if (hopDong.SoTienVay <= 0 || hopDong.LaiSuat <= 0 || hopDong.KyHanThang <= 0)
            {
                return schedule;
            }

            var principal = hopDong.SoTienVay;
            var monthlyInterestRate = hopDong.LaiSuat / 12 / 100;
            var termInMonths = hopDong.KyHanThang;

            var monthlyPayment = termInMonths > 0 ? (principal * monthlyInterestRate * (decimal)Math.Pow(1 + (double)monthlyInterestRate, termInMonths)) / ((decimal)Math.Pow(1 + (double)monthlyInterestRate, termInMonths) - 1) : principal;

            var remainingBalance = principal;
            for (int i = 1; i <= termInMonths; i++)
            {
                var interestForMonth = remainingBalance * monthlyInterestRate;
                var principalForMonth = monthlyPayment - interestForMonth;
                remainingBalance -= principalForMonth;
                if (i == termInMonths)
                {
                    principalForMonth += remainingBalance;
                    monthlyPayment = principalForMonth + interestForMonth;
                    remainingBalance = 0;
                }
                schedule.Add(new LichTraNo
                {
                    MaHopDong = hopDong.MaHopDong,
                    KyHanThu = i,
                    NgayTra = hopDong.NgayVay?.AddMonths(i),
                    SoTienGoc = Math.Round(principalForMonth, 2),
                    SoTienLai = Math.Round(interestForMonth, 2),
                    SoTienPhaiTra = Math.Round(monthlyPayment, 2),
                    TrangThai = "Chưa trả"
                });
            }
            return schedule;
        }
        public async Task<IActionResult> NoQuaHan()
        {
            // Lấy ngày hiện tại để so sánh
            var homNay = DateOnly.FromDateTime(DateTime.Now);

            var danhSachNoQuaHan = await _context.LichTraNos
                                        .Include(l => l.MaHopDongNavigation.MaKhNavigation) // Lấy thông tin Hợp đồng -> Khách hàng
                                        .Where(l => l.TrangThai != "Đã trả" && l.NgayTra < homNay)
                                        .OrderBy(l => l.NgayTra) // Sắp xếp theo ngày quá hạn cũ nhất lên đầu
                                        .ToListAsync();

            return View(danhSachNoQuaHan); // Trả về View NoQuaHan.cshtml
        }

    }
}
