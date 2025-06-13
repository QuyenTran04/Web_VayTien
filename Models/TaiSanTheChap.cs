using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho một tài sản được thế chấp để đảm bảo cho một hợp đồng vay.
/// </summary>
public partial class TaiSanTheChap
{
    [Key]
    public int MaTs { get; set; }

    // Một tài sản thế chấp phải thuộc về một hợp đồng vay
    [Required]
    public int MaHopDong { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên tài sản.")]
    [StringLength(200)]
    [Display(Name = "Tên tài sản")]
    public string TenTaiSan { get; set; }

    [Column(TypeName = "decimal(18, 2)")] // Định dạng kiểu decimal trong DB
    [Display(Name = "Giá trị ước tính")]
    public decimal? GiaTri { get; set; }

    [StringLength(1000)]
    [Display(Name = "Mô tả tài sản")]
    public string? MoTa { get; set; }

    [StringLength(50)]
    [Display(Name = "Tình trạng tài sản")]
    public string? TinhTrang { get; set; } // Ví dụ: "Đã thẩm định", "Chờ thẩm định"

    // Navigation property
    [ForeignKey("MaHopDong")]
    public virtual HopDongVay? MaHopDongNavigation { get; set; }
}
