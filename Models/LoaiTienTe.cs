using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VAYTIEN.Models;

/// <summary>
/// Đại diện cho một loại tiền tệ (ví dụ: VND, USD).
/// </summary>
public partial class LoaiTienTe
{
   
    [Key]
    public int MaLoaiTienTe { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên loại tiền tệ.")]
    [StringLength(50)]
    [Display(Name = "Tên Loại tiền tệ")]
    public string TenLoaiTienTe { get; set; }

    // Navigation property: Một loại tiền tệ có thể được sử dụng trong nhiều hợp đồng vay.
    public virtual ICollection<HopDongVay> HopDongVays { get; set; } = new List<HopDongVay>();
}
