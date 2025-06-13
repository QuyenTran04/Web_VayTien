using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho một giao dịch tài chính được ghi nhận trong hệ thống.
/// Ví dụ: Giải ngân khoản vay, khách hàng trả nợ...
/// </summary>
public partial class GiaoDich
{
    [Key]
    public int MaGiaoDich { get; set; }

    // Một giao dịch phải thuộc về một tài khoản
    [Required]
    [Display(Name = "Tài khoản Ngân hàng")]
    public int MaTaiKhoan { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập ngày giao dịch.")]
    [Display(Name = "Ngày Giao dịch")]
    public DateOnly NgayGd { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số tiền.")]
    [Column(TypeName = "decimal(18, 2)")] // Định dạng kiểu decimal trong DB
    [Display(Name = "Số tiền Giao dịch")]
    public decimal SoTienGd { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập loại giao dịch.")]
    [StringLength(50)]
    [Display(Name = "Loại Giao dịch")]
    public string LoaiGd { get; set; } // Ví dụ: "Giải ngân", "Thu nợ", "Rút tiền"...

    [StringLength(255)]
    [Display(Name = "Nội dung")]
    public string? NoiDungGd { get; set; }

    // Navigation property
    [ForeignKey("MaTaiKhoan")]
    public virtual TaiKhoanNganHang? MaTaiKhoanNavigation { get; set; }
}
