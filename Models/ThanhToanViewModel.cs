using System.ComponentModel.DataAnnotations;

namespace VAYTIEN.Models
{
    /// <summary>
    /// ViewModel dùng để truyền dữ liệu cho trang xác nhận và xử lý thanh toán.
    /// </summary>
    public class ThanhToanViewModel
    {
        [Required]
        public int MaHopDong { get; set; }

        [Required]
        [Display(Name = "Kỳ hạn thanh toán")]
        public int KyHan { get; set; }

        [Display(Name = "Tên khách hàng")]
        public string TenKhachHang { get; set; } = string.Empty;

        [Display(Name = "Ngày đến hạn")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime NgayTra { get; set; }

        [Required]
        [Display(Name = "Số tiền phải trả")]
        [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
        public decimal SoTienPhaiTra { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "Chưa trả";

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public string? PhuongThuc { get; set; } // Ví dụ: "Momo", "VNPAY"
                                                // THUỘC TÍNH MỚI
        public decimal TienPhat { get; set; } = 0; // Số tiền phạt
        public int SoNgayTre { get; set; } = 0;   // Số ngày trả trễ
    }
}
