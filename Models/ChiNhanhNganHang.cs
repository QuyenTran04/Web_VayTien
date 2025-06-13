using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho một chi nhánh ngân hàng trong hệ thống.
/// </summary>
public partial class ChiNhanhNganHang
{
    [Key] // Đánh dấu đây là khóa chính
    public int MaChiNhanh { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chi nhánh.")]
    [StringLength(150, ErrorMessage = "Tên chi nhánh không được vượt quá 150 ký tự.")]
    [Display(Name = "Tên Chi nhánh")]
    public string TenChiNhanh { get; set; }

    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? Sdt { get; set; }

    // Navigation property: Một chi nhánh có nhiều nhân viên
    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
