using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho thông tin hồ sơ của một khách hàng trong hệ thống.
/// </summary>
public partial class KhachHang
{
    [Key]
    public int MaKh { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100)]
    [Display(Name = "Họ và Tên")]
    public string HoTen { get; set; }

    [StringLength(12, MinimumLength = 9, ErrorMessage = "CMND/CCCD phải có từ 9 đến 12 ký tự.")]
    [Display(Name = "CMND/CCCD")]
    public string? CmndCccd { get; set; }

    [Display(Name = "Ngày sinh")]
    public DateOnly? NgaySinh { get; set; }

    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string Sdt { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
    [StringLength(100)]
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
    public string Email { get; set; }

    [StringLength(100)]
    [Display(Name = "Nghề nghiệp")]
    public string? NgheNghiep { get; set; }

    [StringLength(50)]
    [Display(Name = "Tình trạng hôn nhân")]
    public string? TinhTrangHonNhan { get; set; }

    [Display(Name = "Đối tượng vay")]
    public int? DoiTuongVayMaDoiTuongVay { get; set; }

    [StringLength(255)]
    [Display(Name = "Ảnh đính kèm")]
    public string? AnhDinhKem { get; set; }


    // === Navigation Properties ===

    [ForeignKey("DoiTuongVayMaDoiTuongVay")]
    public virtual DoiTuongVay? DoiTuongVay { get; set; } // Thêm navigation property còn thiếu

    public virtual ICollection<HopDongVay> HopDongVays { get; set; } = new List<HopDongVay>();

    public virtual ICollection<TaiKhoanNganHang> TaiKhoanNganHangs { get; set; } = new List<TaiKhoanNganHang>();
}
