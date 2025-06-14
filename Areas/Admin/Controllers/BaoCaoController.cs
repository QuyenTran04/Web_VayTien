using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VAYTIEN.Models;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Drawing;
namespace VAYTIEN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới có quyền xem báo cáo
    public class BaoCaoController : Controller
    {
        private readonly QlvayTienContext _context;

        public BaoCaoController(QlvayTienContext context)
        {
            _context = context;
        }

        // GET: Admin/BaoCao/DoanhThuTheoThang
        public async Task<IActionResult> DoanhThuTheoThang()
        {
            // SỬA LỖI: Chuyển đổi ngày tháng ở phía C# trước khi truy vấn
            var twelveMonthsAgo = DateOnly.FromDateTime(DateTime.Now.AddMonths(-12));

            var monthlyRevenue = await _context.LichTraNos
                // SỬA LỖI: So sánh trực tiếp DateOnly với DateOnly
                .Where(l => l.TrangThai == "Đã trả" && l.NgayTra.HasValue && l.SoTienLai > 0 && l.NgayTra.Value >= twelveMonthsAgo)
                .GroupBy(l => new { Year = l.NgayTra.Value.Year, Month = l.NgayTra.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalInterest = g.Sum(l => l.SoTienLai ?? 0)
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();

            // Chuẩn bị dữ liệu cho biểu đồ
            var labels = monthlyRevenue.Select(r => $"T{r.Month}/{r.Year}").ToList();
            var data = monthlyRevenue.Select(r => r.TotalInterest).ToList();

            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            return View();
        }

        public async Task<IActionResult> BaoCaoDoanhThu(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            toDate ??= DateTime.Now;

            // SỬA LỖI: Sử dụng Include và ThenInclude để tải dữ liệu liên quan một cách tường minh
            var revenueData = await _context.LichTraNos
                .Include(l => l.MaHopDongNavigation)       // Tải thông tin Hợp đồng
                    .ThenInclude(hd => hd.MaKhNavigation) // Từ Hợp đồng, tải tiếp thông tin Khách hàng
                .Where(l => l.TrangThai == "Đã trả" &&
                             l.NgayTra >= DateOnly.FromDateTime(fromDate.Value) &&
                             l.NgayTra <= DateOnly.FromDateTime(toDate.Value))
                .OrderByDescending(l => l.NgayTra)
                .ToListAsync();

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.TotalRevenue = revenueData.Sum(l => (l.SoTienPhaiTra ?? 0) + (l.SoTienPhat ?? 0));

            return View(revenueData);
        }

        // GET: /Admin/BaoCao/ExportDoanhThuToExcel
        public async Task<IActionResult> ExportDoanhThuToExcel(DateTime fromDate, DateTime toDate)
        {
            var revenueData = await _context.LichTraNos
                .Include(l => l.MaHopDongNavigation.MaKhNavigation)
                .Where(l => l.TrangThai == "Đã trả" &&
                             l.NgayTra >= DateOnly.FromDateTime(fromDate) &&
                             l.NgayTra <= DateOnly.FromDateTime(toDate))
                .OrderByDescending(l => l.NgayTra)
                .ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DoanhThu");

                worksheet.Cells["A1:F1"].Merge = true;
                worksheet.Cells["A1"].Value = $"BÁO CÁO DOANH THU TỪ {fromDate:dd/MM/yyyy} ĐẾN {toDate:dd/MM/yyyy}";
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A3"].Value = "Mã HĐ";
                worksheet.Cells["B3"].Value = "Kỳ trả";
                worksheet.Cells["C3"].Value = "Ngày Thanh Toán";
                worksheet.Cells["D3"].Value = "Khách hàng";
                worksheet.Cells["E3"].Value = "Tiền phạt";
                worksheet.Cells["F3"].Value = "Tổng thu";

                using (var range = worksheet.Cells["A3:F3"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                int row = 4;
                foreach (var item in revenueData)
                {
                    worksheet.Cells[row, 1].Value = item.MaHopDong;
                    worksheet.Cells[row, 2].Value = item.KyHanThu;
                    worksheet.Cells[row, 3].Value = item.NgayTra?.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 4].Value = item.MaHopDongNavigation?.MaKhNavigation?.HoTen;
                    worksheet.Cells[row, 5].Value = item.SoTienPhat ?? 0;
                    worksheet.Cells[row, 6].Value = (item.SoTienPhaiTra ?? 0) + (item.SoTienPhat ?? 0);
                    row++;
                }

                worksheet.Column(5).Style.Numberformat.Format = "#,##0";
                worksheet.Column(6).Style.Numberformat.Format = "#,##0";

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}
