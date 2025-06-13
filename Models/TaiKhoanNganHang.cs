using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho một tài khoản ngân hàng trong hệ thống.
/// Có thể thuộc về một khách hàng (có MaKh) hoặc thuộc về công ty (MaKh là null).
/// </summary>
public partial class TaiKhoanNganHang
{
   
    public int MaTaiKhoan { get; set; }

    [Display(Name = "Khách hàng")]
    public int? MaKh { get; set; } // Nullable vì tài khoản của công ty không có MaKh

    [Required(ErrorMessage = "Vui lòng nhập số tài khoản.")]
    [StringLength(20)]
    [Display(Name = "Số tài khoản")]
    public string SoTaiKhoan { get; set; }

    [StringLength(100)]
    [Display(Name = "Loại tài khoản")]
    public string? LoaiTaiKhoan { get; set; } // Ví dụ: Thanh toán, Tín dụng...

    [Required]
    [Column(TypeName = "decimal(18, 2)")] // Định dạng kiểu decimal trong DB
    [Display(Name = "Số dư")]
    public decimal SoDu { get; set; } = 0; // Mặc định số dư là 0

    [StringLength(50)]
    [Display(Name = "Trạng thái")]
    public string? TrangThai { get; set; } // Ví dụ: "Hoạt động", "Bị khóa"

    // === Navigation Properties ===

    public virtual ICollection<GiaoDich> GiaoDiches { get; set; } = new List<GiaoDich>();

    [ForeignKey("MaKh")]
    public virtual KhachHang? MaKhNavigation { get; set; }
}
