using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho một lần thanh toán cụ thể cho một kỳ trả nợ.
/// Bảng này dùng để ghi log chi tiết các giao dịch thanh toán.
/// </summary>
public partial class ThanhToanLichTra
{
    [Key]
    public int MaThanhToan { get; set; }

    // Một lần thanh toán phải thuộc về một kỳ trả nợ cụ thể
    [Required]
    [Display(Name = "Kỳ trả nợ")]
    public int MaLich { get; set; }

    [Display(Name = "Ngày thanh toán")]
    public DateOnly? NgayThanhToan { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán.")]
    [Column(TypeName = "decimal(18, 2)")] // Định dạng kiểu decimal trong DB
    [Display(Name = "Số tiền đã thanh toán")]
    public decimal SoTienThanhToan { get; set; }

    [StringLength(100)]
    [Display(Name = "Hình thức thanh toán")]
    public string? HinhThucThanhToan { get; set; } // Ví dụ: "MoMo", "VNPAY", "Tiền mặt"

    [StringLength(50)]
    [Display(Name = "Mã giao dịch (Nếu có)")]
    public string? SoTaiKhoanGd { get; set; }

    // Navigation property
    [ForeignKey("MaLich")]
    public virtual LichTraNo? MaLichNavigation { get; set; }
}
