namespace VAYTIEN.Models
{
    /// <summary>
    /// ViewModel được sử dụng cho trang hiển thị lỗi (Error Page).
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Chứa mã định danh duy nhất của yêu cầu (request) gây ra lỗi.
        /// Hữu ích cho việc theo dõi và gỡ lỗi (debug).
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Một thuộc tính chỉ đọc, trả về 'true' nếu RequestId có giá trị.
        /// Dùng để quyết định có hiển thị mã Request ID trên trang lỗi hay không.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
