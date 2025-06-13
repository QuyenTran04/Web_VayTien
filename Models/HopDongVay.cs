using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VAYTIEN.Models
{
    public partial class HopDongVay
    {
        [Key]
        public int MaHopDong { get; set; }

        [Required]
        public int MaKh { get; set; }

        [Required]
        public int? MaLoaiVay { get; set; }

        public int? MaLoaiTienTe { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTienVay { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? SoTienConLai { get; set; }

        public DateOnly? NgayVay { get; set; }
        public DateOnly? NgayHetHan { get; set; }

        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal LaiSuat { get; set; }

        [StringLength(100)]
        public string? HinhThucTra { get; set; }

        public int? MaNv { get; set; }

        [StringLength(50)]
        public string? TinhTrang { get; set; }

        [Required]
        public int KyHanThang { get; set; }

        // Navigation Properties
        [ForeignKey("MaKh")]
        public virtual KhachHang? MaKhNavigation { get; set; }
        [ForeignKey("MaLoaiTienTe")]
        public virtual LoaiTienTe? MaLoaiTienTeNavigation { get; set; }
        [ForeignKey("MaLoaiVay")]
        public virtual LoaiVay? MaLoaiVayNavigation { get; set; }
        [ForeignKey("MaNv")]
        public virtual NhanVien? MaNvNavigation { get; set; }

        public virtual ICollection<LichTraNo> LichTraNos { get; set; } = new List<LichTraNo>();
        public virtual ICollection<TaiSanTheChap> TaiSanTheChaps { get; set; } = new List<TaiSanTheChap>();
    }
}