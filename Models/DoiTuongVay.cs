using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models
{
    /// <summary>
    /// Đại diện cho một đối tượng khách hàng (ví dụ: Sinh viên, Công chức) 
    /// và mức lãi suất cơ bản áp dụng cho đối tượng đó.
    /// </summary>
    public class DoiTuongVay
    {
        [Key]
        public int MaDoiTuongVay { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đối tượng vay.")]
        [MaxLength(100)]
        [Display(Name = "Tên Đối tượng")]
        public string TenDoiTuong { get; set; }

        /// <summary>
        /// Lãi suất cơ bản áp dụng cho đối tượng này (tính theo %/năm).
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập lãi suất.")]
        [Column(TypeName = "decimal(5, 2)")] // Chính xác cho 2 chữ số thập phân, vd: 12.50
        [Display(Name = "Lãi suất (%/năm)")]
        [Range(0, 100, ErrorMessage = "Lãi suất phải từ 0 đến 100.")]
        public decimal LaiSuat { get; set; }

        // Navigation property: Một đối tượng vay có thể có nhiều khách hàng
        public virtual ICollection<KhachHang> KhachHangs { get; set; } = new List<KhachHang>();
    }
}
