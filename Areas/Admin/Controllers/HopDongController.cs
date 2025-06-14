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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoi(int id, string lyDo) // Nhận thêm tham số "lyDo" từ form trong Modal
        {
            // Lấy thông tin hợp đồng, phải Include() cả KhachHang để có Email và Tên
            var hd = await _context.HopDongVays
                                .Include(h => h.MaKhNavigation)
                                .FirstOrDefaultAsync(h => h.MaHopDong == id);

            if (hd == null || hd.TinhTrang != "Chờ phê duyệt")
            {
                TempData["Error"] = "Hợp đồng không hợp lệ hoặc đã được xử lý.";
                return RedirectToAction(nameof(ChoPheDuyet));
            }

            // 1. Cập nhật trạng thái hợp đồng
            hd.TinhTrang = "Đã từ chối";
            // Bạn có thể thêm một cột GhiChu vào HopDongVay để lưu lý do nếu muốn
            // hd.GhiChu = lyDo; 
            _context.Update(hd);
            await _context.SaveChangesAsync();

            // 2. Soạn nội dung và gửi email thông báo từ chối
            var emailBody = $@"
        <p>Kính gửi Quý khách <strong>{hd.MaKhNavigation.HoTen}</strong>,</p>
        <p>Chúng tôi rất tiếc phải thông báo yêu cầu vay vốn của Quý khách (Hợp đồng #{hd.MaHopDong}) đã không được phê duyệt.</p>
        <p><strong>Lý do:</strong> {lyDo}</p>
        <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chi nhánh gần nhất để được hỗ trợ.</p>
        <br/>
        <p>Trân trọng,</p>
        <p><strong>Ngân hàng VAYTIEN</strong></p>";

            try
            {
                await _emailSender.SendEmailAsync(hd.MaKhNavigation.Email!, "Thông báo về Kết quả Yêu cầu Vay vốn", emailBody);
                TempData["Success"] = $"Đã từ chối hợp đồng #{id} và gửi email thông báo thành công.";
            }
            catch (Exception ex)
            {
                // Ghi log lỗi gửi mail nếu cần, nhưng vẫn báo thành công việc từ chối
                TempData["Warning"] = $"Đã từ chối hợp đồng #{id}, nhưng có lỗi xảy ra khi gửi email: " + ex.Message;
            }

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiNhacNho(int maLich)
        {
            var lichTra = await _context.LichTraNos
                                .Include(l => l.MaHopDongNavigation.MaKhNavigation)
                                .FirstOrDefaultAsync(l => l.MaLich == maLich);

            if (lichTra == null)
            {
                TempData["Error"] = "Không tìm thấy kỳ hạn nợ để gửi nhắc nhở.";
                return RedirectToAction(nameof(NoQuaHan));
            }

            var khachHang = lichTra.MaHopDongNavigation.MaKhNavigation;
            var hopDong = lichTra.MaHopDongNavigation;
            var homNay = DateOnly.FromDateTime(DateTime.Now);
            var soNgayTre = homNay.DayNumber - (lichTra.NgayTra?.DayNumber ?? homNay.DayNumber);

            // Soạn nội dung email nhắc nợ
            var emailBody = $@"
        <p>Kính gửi Quý khách <strong>{khachHang.HoTen}</strong>,</p>
        <p>Ngân hàng VAYTIEN trân trọng thông báo về khoản thanh toán quá hạn của Quý khách:</p>
        <ul>
            <li><strong>Hợp đồng số:</strong> #{hopDong.MaHopDong}</li>
            <li><strong>Kỳ hạn thanh toán thứ:</strong> {lichTra.KyHanThu}</li>
            <li><strong>Ngày đến hạn:</strong> {lichTra.NgayTra:dd/MM/yyyy}</li>
            <li><strong>Số tiền cần thanh toán:</strong> {lichTra.SoTienPhaiTra:N0} VNĐ</li>
            <li><strong>Số ngày quá hạn:</strong> {soNgayTre} ngày</li>
        </ul>
        <p>Để tránh phát sinh các khoản phí phạt không mong muốn, Quý khách vui lòng thực hiện thanh toán cho kỳ hạn này trong thời gian sớm nhất.</p>
        <p>Nếu Quý khách đã thanh toán, vui lòng bỏ qua thông báo này. Xin cảm ơn!</p>
        <br/>
        <p>Trân trọng,</p>
        <p><strong>Ngân hàng VAYTIEN</strong></p>";

            try
            {
                await _emailSender.SendEmailAsync(
                    khachHang.Email,
                    $"Thông báo Nhắc nợ Hợp đồng #{hopDong.MaHopDong}",
                    emailBody
                );
                TempData["Success"] = $"Đã gửi email nhắc nợ thành công cho khách hàng {khachHang.HoTen}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi gửi email: " + ex.Message;
            }

            return RedirectToAction(nameof(NoQuaHan));
        }


    }
}