using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using VAYTIEN.Models;
using VAYTIEN.Services;

namespace VAYTIEN.Controllers
{
    [Authorize]
    public class ThanhToanController : Controller
    {
        private readonly QlvayTienContext _context;
        private readonly MoMoService _momoService;
        private readonly VnpayService _vnpayService;
        private readonly IConfiguration _configuration;

        public ThanhToanController(QlvayTienContext context, MoMoService momoService, VnpayService vnpayService, IConfiguration configuration)
        {
            _context = context;
            _momoService = momoService;
            _vnpayService = vnpayService;
            _configuration = configuration;
        }

        // GET: /ThanhToan/ChiTiet?maHopDong=...&kyHanThu=...
        // Hiển thị trang xác nhận thanh toán.
        [HttpGet]
        public async Task<IActionResult> ChiTiet(int maHopDong, int kyHanThu)
        {
            var lichTra = await _context.LichTraNos
                .Include(l => l.MaHopDongNavigation.MaKhNavigation)
                .FirstOrDefaultAsync(x => x.MaHopDong == maHopDong && x.KyHanThu == kyHanThu);

            if (lichTra?.MaHopDongNavigation?.MaKhNavigation == null)
            {
                return NotFound();
            }
            if (lichTra.MaHopDongNavigation.MaKhNavigation.Email != User.Identity?.Name)
            {
                return Forbid(); // Ngăn người dùng xem thanh toán của người khác
            }

            var viewModel = new ThanhToanViewModel
            {
                MaHopDong = maHopDong,
                KyHan = kyHanThu,
                TenKhachHang = lichTra.MaHopDongNavigation.MaKhNavigation.HoTen,
                NgayTra = lichTra.NgayTra?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today,
                SoTienPhaiTra = lichTra.SoTienPhaiTra ?? 0,
                TrangThai = lichTra.TrangThai ?? "Chưa trả"
            };

            // Tính toán tiền phạt nếu có
            var homNay = DateOnly.FromDateTime(DateTime.Now);
            if (lichTra.TrangThai != "Đã trả" && lichTra.NgayTra < homNay)
            {
                var soNgayTre = homNay.DayNumber - lichTra.NgayTra.Value.DayNumber;
                var penaltyRate = _configuration.GetValue<decimal>("PenaltySettings:DailyRate");
                var tienPhat = (lichTra.SoTienGoc ?? 0) * penaltyRate * soNgayTre;
                viewModel.SoNgayTre = soNgayTre;
                viewModel.TienPhat = Math.Round(tienPhat, 0);
            }

            return View(viewModel);
        }

        // POST: /ThanhToan/ThucHien
        // Tạo yêu cầu thanh toán đến cổng thanh toán đã chọn.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThucHien(ThanhToanViewModel model)
        {
            var lichTra = await _context.LichTraNos
                .Include(l => l.MaHopDongNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaHopDong == model.MaHopDong && x.KyHanThu == model.KyHan);

            if (lichTra == null) return NotFound();
            if (lichTra.TrangThai == "Đã trả")
            {
                TempData["Error"] = "Kỳ hạn này đã được thanh toán.";
                return RedirectToAction("ChiTiet", new { maHopDong = model.MaHopDong, kyHanThu = model.KyHan });
            }

            decimal totalAmount = model.SoTienPhaiTra + model.TienPhat;

            // Kiểm tra số dư của khách hàng trước khi tạo yêu cầu
            var customerAccount = await _context.TaiKhoanNganHangs.FirstOrDefaultAsync(tk => tk.MaKh == lichTra.MaHopDongNavigation.MaKh);
            if (customerAccount == null || customerAccount.SoDu < totalAmount)
            {
                TempData["Error"] = "Số dư tài khoản không đủ để thực hiện giao dịch.";
                return RedirectToAction("ChiTiet", new { maHopDong = model.MaHopDong, kyHanThu = model.KyHan });
            }

            string orderId = $"{model.MaHopDong}_{model.KyHan}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            string orderInfo = $"Thanh toan HD#{model.MaHopDong} Ky#{model.KyHan}";

            if (model.PhuongThuc == "Momo")
            {
                string returnUrl = Url.Action("MoMoReturn", "ThanhToan", null, Request.Scheme)!;
                string notifyUrl = Url.Action("MoMoNotify", "ThanhToan", null, Request.Scheme)!;
                var payUrl = await _momoService.CreatePaymentAsync(orderId, (long)totalAmount, orderInfo, returnUrl, notifyUrl);
                return Redirect(payUrl);
            }
            else if (model.PhuongThuc == "VNPAY")
            {
                string returnUrl = Url.Action("VnpayReturn", "ThanhToan", null, Request.Scheme)!;
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var payUrl = _vnpayService.CreatePaymentUrl((long)totalAmount, orderId, orderInfo, clientIp);
                return Redirect(payUrl);
            }

            TempData["Error"] = "Vui lòng chọn một phương thức thanh toán hợp lệ.";
            return RedirectToAction("ThongTinVay", "KhachHang");
        }

        // Xử lý callback từ MoMo
        public async Task<IActionResult> MoMoReturn()
        {
            var orderId = Request.Query["orderId"].ToString();
            var resultCode = Request.Query["resultCode"].ToString();
            if (resultCode == "0")
            {
                await ProcessSuccessfulPaymentAndUpdateDbAsync(orderId);
                TempData["Success"] = "Thanh toán thành công!";
            }
            else
            {
                TempData["Error"] = "Thanh toán không thành công hoặc bị hủy.";
            }
            return RedirectToAction("ThongTinVay", "KhachHang");
        }

        // Xử lý IPN từ MoMo
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> MoMoNotify()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            dynamic? notifyData = JsonConvert.DeserializeObject(body);
            if (notifyData != null && notifyData.resultCode == "0")
            {
                string orderId = notifyData.orderId;
                await ProcessSuccessfulPaymentAndUpdateDbAsync(orderId);
            }
            return Ok();
        }

        // Xử lý callback từ VNPay
        public async Task<IActionResult> VnpayReturn()
        {
            var queryCollection = Request.Query;
            if (queryCollection.Count > 0 && queryCollection.ContainsKey("vnp_ResponseCode") && queryCollection["vnp_ResponseCode"] == "00")
            {
                var orderId = queryCollection["vnp_TxnRef"].ToString();
                await ProcessSuccessfulPaymentAndUpdateDbAsync(orderId);
                TempData["Success"] = "Thanh toán VNPay thành công!";
            }
            else
            {
                TempData["Error"] = "Thanh toán VNPay không thành công hoặc đã bị hủy.";
            }
            return RedirectToAction("ThongTinVay", "KhachHang");
        }


        // Hàm xử lý trung tâm sau khi có xác nhận thanh toán thành công
        private async Task<bool> ProcessSuccessfulPaymentAndUpdateDbAsync(string orderId)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ids = orderId.Split('_');
                if (ids.Length < 2) return false;
                int maHopDong = int.Parse(ids[0]);
                int kyHan = int.Parse(ids[1]);

                var lichTra = await _context.LichTraNos
                    .Include(l => l.MaHopDongNavigation)
                    .FirstOrDefaultAsync(x => x.MaHopDong == maHopDong && x.KyHanThu == kyHan);

                if (lichTra == null || lichTra.TrangThai == "Đã trả")
                {
                    await dbTransaction.RollbackAsync();
                    return false;
                }

                var companyAccount = await _context.TaiKhoanNganHangs.FirstOrDefaultAsync(tk => tk.SoTaiKhoan == _configuration["AppSettings:CompanyBankAccountNumber"]);
                var customerAccount = await _context.TaiKhoanNganHangs.FirstOrDefaultAsync(tk => tk.MaKh == lichTra.MaHopDongNavigation.MaKh);

                if (companyAccount == null || customerAccount == null)
                {
                    await dbTransaction.RollbackAsync();
                    return false;
                }

                // Tính toán lại tiền phạt tại thời điểm xử lý
                decimal tienPhat = 0;
                var homNay = DateOnly.FromDateTime(DateTime.Now);
                if (lichTra.NgayTra < homNay)
                {
                    var soNgayTre = homNay.DayNumber - lichTra.NgayTra.Value.DayNumber;
                    var penaltyRate = _configuration.GetValue<decimal>("PenaltySettings:DailyRate");
                    tienPhat = (lichTra.SoTienGoc ?? 0) * penaltyRate * soNgayTre;
                }
                decimal totalPayment = (lichTra.SoTienPhaiTra ?? 0) + Math.Round(tienPhat, 0);

                if (customerAccount.SoDu < totalPayment)
                {
                    await dbTransaction.RollbackAsync();
                    return false;
                }

                // 1. Trừ tiền tài khoản khách hàng
                customerAccount.SoDu -= totalPayment;

                // 2. Cộng tiền vào tài khoản công ty/admin
                companyAccount.SoDu += totalPayment;

                // 3. Cập nhật trạng thái kỳ trả nợ và số tiền phạt
                lichTra.TrangThai = "Đã trả";
                lichTra.NgayTra = homNay;
                lichTra.SoTienPhat = Math.Round(tienPhat, 0);

                // 4. Cập nhật số nợ gốc còn lại của hợp đồng
                var hopDong = lichTra.MaHopDongNavigation;
                if (hopDong != null)
                {
                    hopDong.SoTienConLai -= (lichTra.SoTienGoc ?? 0);
                }

                // 5. Ghi nhận 2 giao dịch
                var debitTransaction = new GiaoDich { MaTaiKhoan = customerAccount.MaTaiKhoan, NgayGd = homNay, SoTienGd = -totalPayment, LoaiGd = "Thanh toán nợ", NoiDungGd = $"Thanh toán HĐ #{maHopDong}, kỳ {kyHan}" };
                _context.GiaoDiches.Add(debitTransaction);

                var creditTransaction = new GiaoDich { MaTaiKhoan = companyAccount.MaTaiKhoan, NgayGd = homNay, SoTienGd = totalPayment, LoaiGd = "Thu nợ", NoiDungGd = $"Nhận thanh toán HĐ #{maHopDong}, kỳ {kyHan}" };
                _context.GiaoDiches.Add(creditTransaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                return false;
            }
        }
    }
}
