using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Định nghĩa một sản phẩm/loại hình cho vay của ngân hàng.
/// </summary>
public partial class LoaiVay
{
    [Key]
    public int MaLoaiVay { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên loại vay.")]
    [StringLength(150)]
    [Display(Name = "Tên Loại Vay")]
    public string TenLoaiVay { get; set; }

    /// <summary>
    /// Lãi suất mặc định cho loại vay này (tính theo %/năm).
    /// </summary>
    [Column(TypeName = "decimal(5, 2)")] // Kiểu dữ liệu trong DB, ví dụ: 12.50
    [Display(Name = "Lãi suất (%/năm)")]
    public decimal? LaiSuat { get; set; } // Sửa từ double? sang decimal?

    [Display(Name = "Kỳ hạn Tối đa (tháng)")]
    public int? KyHan { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    // Navigation property
    public virtual ICollection<HopDongVay> HopDongVays { get; set; } = new List<HopDongVay>();
}
