namespace VAYTIEN.Models
{
    /// <summary>
    /// ViewModel dùng riêng cho Form đăng ký vay ở Bước 2.
    /// </summary>
    public class CreateStep2ViewModel
    {
        // Chứa thông tin Hợp đồng vay
        public HopDongVay HopDong { get; set; }

        // Chứa thông tin Tài sản thế chấp
        public TaiSanTheChap TaiSan { get; set; }
    }
}