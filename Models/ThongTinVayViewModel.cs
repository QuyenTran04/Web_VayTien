using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VAYTIEN.Models
{
    public class ThongTinVayViewModel
    {
        public decimal TongSoTienVay { get; set; }

        // THÊM MỚI: Danh sách tài khoản của khách hàng
        public List<TaiKhoanViewModel> TaiKhoans { get; set; } = new List<TaiKhoanViewModel>();
        public List<ThongTinHopDongViewModel> HopDongs { get; set; } = new List<ThongTinHopDongViewModel>();
    }
    public class TaiKhoanViewModel
    {
        public string SoTaiKhoan { get; set; }
        public decimal SoDu { get; set; }
    }
    public class ThongTinHopDongViewModel
    {
        public int MaHopDong { get; set; }
        public decimal? SoTienVay { get; set; }
        public DateOnly? NgayVay { get; set; }
        public DateOnly? NgayHetHan { get; set; }
        public decimal? SoTienConLai { get; set; }
        public int? KyHanThang { get; set; }
        public decimal? LaiSuat { get; set; }
        public string? TinhTrang { get; set; }
        public List<LichTraViewModel> LichTra { get; set; } = new List<LichTraViewModel>();
    }

    public class LichTraViewModel
    {
        public int KyHanThu { get; set; }
        public DateOnly? NgayTra { get; set; }
        public decimal? SoTienPhaiTra { get; set; }
        public string? TrangThai { get; set; }
    }
}