using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho một kỳ trả nợ cụ thể trong lịch trả nợ của một hợp đồng.
/// </summary>
public partial class LichTraNo
{
    [Key]
    public int MaLich { get; set; }

    // Một kỳ trả nợ phải thuộc về một hợp đồng
    [Required]
    public int MaHopDong { get; set; }

    [Display(Name = "Kỳ hạn thứ")]
    public int? KyHanThu { get; set; }

    [Display(Name = "Ngày đến hạn")]
    public DateOnly? NgayTra { get; set; }

    // THUỘC TÍNH ĐƯỢC THÊM VÀO: Tiền gốc phải trả trong kỳ
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Tiền gốc kỳ này")]
    public decimal? SoTienGoc { get; set; }
    // THUỘC TÍNH MỚI: Dùng để lưu số tiền phạt khi thanh toán trễ
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Tiền phạt trễ hạn")]
    public decimal? SoTienPhat { get; set; }

    // THUỘC TÍNH ĐƯỢC THÊM VÀO: Tiền lãi phải trả trong kỳ
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Tiền lãi kỳ này")]
    public decimal? SoTienLai { get; set; }

    /// <summary>
    /// Tổng số tiền phải trả trong kỳ (Gốc + Lãi)
    /// </summary>
    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Tổng tiền phải trả")]
    public decimal? SoTienPhaiTra { get; set; }

    [StringLength(50)]
    [Display(Name = "Trạng thái")]
    public string? TrangThai { get; set; }

    [ForeignKey("MaHopDong")]
    public virtual HopDongVay? MaHopDongNavigation { get; set; }

    public virtual ICollection<ThanhToanLichTra> ThanhToanLichTras { get; set; } = new List<ThanhToanLichTra>();
}
