using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho thông tin HỒ SƠ của một nhân viên.
/// Hồ sơ này được liên kết với một TÀI KHOẢN ApplicationUser.
/// </summary>
public partial class NhanVien
{
    [Key]
    public int MaNv { get; set; }

    // THÊM MỚI: Khóa ngoại để liên kết với tài khoản đăng nhập ApplicationUser
    [Required]
    public string ApplicationUserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên nhân viên.")]
    [StringLength(100)]
    [Display(Name = "Tên Nhân viên")]
    public string TenNv { get; set; }

    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? Sdt { get; set; }

    [Display(Name = "Chi nhánh")]
    public int? MaChiNhanh { get; set; }

    [StringLength(100)]
    [Display(Name = "Chức vụ")]
    public string? ChucVu { get; set; } // Ví dụ: "Chuyên viên tín dụng", "Trưởng phòng"...

    // XÓA BỎ các trường không an toàn và không cần thiết
    // public string? Email { get; set; }
    // public string? MatKhau { get; set; }
    // public string? VaiTro { get; set; }


    // === Navigation Properties ===

    [ForeignKey("ApplicationUserId")]
    public virtual ApplicationUser? ApplicationUser { get; set; }

    [ForeignKey("MaChiNhanh")]
    public virtual ChiNhanhNganHang? MaChiNhanhNavigation { get; set; }

    public virtual ICollection<HopDongVay> HopDongVays { get; set; } = new List<HopDongVay>();
}
