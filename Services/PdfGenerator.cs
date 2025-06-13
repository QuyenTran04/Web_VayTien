using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VAYTIEN.Models;
using VAYTIEN.Services;
using System;
using System.IO;

public class PdfGenerator
{
    public string GenerateHopDongTinDungPdf(HopDongVay hd, KhachHang kh)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var folderPath = Path.Combine("wwwroot", "pdf");
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, $"hopdong_tindung_{hd.MaHopDong}.pdf");
            var logoPath = Path.Combine("wwwroot", "images", "Logo.png");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header().Row(row =>
                    {
                        row.ConstantItem(80).Image(logoPath);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(16).AlignCenter();
                            col.Item().Text("Độc lập – Tự do – Hạnh phúc").FontSize(12).AlignCenter();
                            col.Item().Text("———————————–").AlignCenter();
                        });
                    });

                    page.Content().Column(col =>
                    {
                        void AddText(string text, bool bold = false)
                        {
                            var t = col.Item().Text(text).FontSize(11).LineHeight(1.4f);
                            if (bold) t.Bold();
                        }

                        col.Item().Height(10);
                        AddText("HỢP ĐỒNG TÍN DỤNG", true);
                        AddText("Số: 04.");

                        col.Item().Height(10);
                        AddText("Hôm nay, ngày …/… …/ … … … …, tại … … … … … … …, các Bên gồm:");
                        AddText("Bên Cho Vay: Ngân Hàng SacomBank Chi Nhánh Ngãi Giao……………………………………………………….");
                        AddText("Địa chỉ trụ sở : 309A Hùng Vương, Thị trấn Ngãi Giao, Huyện Châu Đức, Tỉnh Bà Rịa-Vũng Tàu..");
                        AddText("Điện thoại: 1800 5858 88 - Fax: (028) 3932 1048");
                        AddText("Đại diện : Ông/Bà: Cao Thanh Lâm- Chức vụ:Quản Lý");
                        AddText("Sau đây gọi là Bên Ngân hàng,");

                        col.Item().Height(5);
                        AddText($"Bên Vay: Ông {kh?.HoTen} ");
                        AddText($"Hộ khẩu TT : {kh?.DiaChi}");
                        AddText($"Địa chỉ hiện tại : {kh?.DiaChi}");
                        AddText($"Giấy CMND : Ông {kh?.HoTen} mang Giấy CMND số {kh?.CmndCccd} do Công an : Cục Cảnh Sát Bà Rịa Vũng Tàu cấp ngày 28/05/2021");
                        AddText($"Điện thoại nhà riêng: {kh?.Sdt} – Điện thoại di động: {kh?.Sdt}");
                        AddText("Email (nếu có): quyentran2909a@gmail.com");

                        col.Item().Height(10);
                        AddText("Đã thỏa thuận và nhất trí ký kết Hợp đồng tín dụng (“Hợp đồng”) này với các nội dung như sau:", true);

                        void AddDieu(string title, string content)
                        {
                            AddText(title, true);
                            AddText(content);
                            col.Item().Height(8);
                        }

                        AddDieu("Điều 1: Bên A đồng ý cho Bên B vay số tiền như sau:",
                            $"- Tổng số tiền cho vay: {hd?.SoTienVay:N0} VNĐ\n" +
                            "- (Bằng chữ):……………………………….\n" +
                            "- Mục đích sử dụng tiền vay:\n" +
                            $"- Phương thức cho vay: {hd?.HinhThucTra}");

                        AddDieu("Điều 2: Thời hạn cho vay:",
                            $"…… tháng. Từ ngày {hd?.NgayVay:dd/MM/yyyy} đến ngày {hd?.NgayHetHan:dd/MM/yyyy}");

                        AddDieu("Điều 3: Lãi suất cho vay, thu lãi tiền vay:",
                            "Lãi suất cho vay: …..%/tháng. Lãi suất nợ quá hạn: ….. %/tháng\n" +
                            "Cách tính lãi tiền vay:\n" +
                            "Thời điểm thu lãi tiền vay:");

                        AddDieu("Điều 4: Thu nợ, phương thức trả nợ:",
                            "Số tiền cho vay được trả thành ….. kỳ hạn. Kỳ hạn trả nợ mức trả nợ mỗi kỳ hạn như sau: ….. tháng thu 1 lần, mỗi lần thu ….. đ vốn, và lãi theo số dư.");

                        AddDieu("Điều 5: Điều kiện nhận tiền vay:",
                            "Bên B chỉ được nhận tiền vay vào mục đích quy định tại Điều 1 của bản hợp đồng.\n" +
                            "Mỗi lần rút tiền vay, Bên B phải xuất trình giấy CMND và các giấy tờ liên quan (nếu có).\n" +
                            "Nếu không trực tiếp nhận tiền vay thì phải có ủy quyền hợp pháp.");

                        AddDieu("Điều 6: Biện pháp đảm bảo tiền vay:",
                            "Cho vay không có bảo đảm bằng tài sản: Thu nợ từ tiền lương và các thu nhập khác được cơ quan xác nhận.");

                        AddDieu("Điều 7: Quyền và nghĩa vụ của Bên A:",
                            "Yêu cầu cung cấp giấy tờ liên quan đến sử dụng vốn vay.\n" +
                            "Từ chối phát tiền hoặc thu hồi nợ khi Bên B vi phạm Điều 4 hoặc sử dụng sai mục đích.\n" +
                            "Nếu không trả đúng hạn, có thể chuyển nợ quá hạn và thông báo cho đơn vị công tác.");

                        AddDieu("Điều 8: Quyền và nghĩa vụ của Bên B:",
                            "Sử dụng vốn đúng mục đích, trả gốc và lãi đúng hạn.\n" +
                            "Cung cấp giấy tờ liên quan, tạo điều kiện kiểm tra, và chịu trách nhiệm nếu vi phạm.");

                        AddDieu("Điều 9: Cam kết chung:",
                            "Hai bên cam kết thực hiện đúng hợp đồng và các quy định pháp luật.\n" +
                            "Nếu có tranh chấp thì thương lượng, nếu không được thì đưa ra cơ quan chức năng.");

                        AddDieu("Điều 10: Hiệu lực hợp đồng:",
                            "Có hiệu lực từ ngày ký đến khi thanh toán đủ gốc và lãi.\n" +
                            "Lập thành 3 bản, mỗi bên giữ một bản có giá trị ngang nhau.");

                        col.Item().Height(20);
                        col.Item().Row(row =>
                        {
                            row.RelativeColumn().Text("BÊN A\n(Ký, ghi rõ họ tên, đóng dấu)").AlignCenter();
                            row.RelativeColumn().Text("BÊN B\n(Ký, ghi rõ họ tên)").AlignCenter();
                        });
                    });
                });
            }).GeneratePdf(filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi khi tạo PDF: " + ex.ToString());
            throw;
        }
    }

    public string GeneratePaymentReceiptPdf(LichTraNo lichTra)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var folderPath = Path.Combine("wwwroot", "receipts");
        Directory.CreateDirectory(folderPath);

        var fileName = $"hoadon_{lichTra.MaHopDong}_{lichTra.KyHanThu}_{Guid.NewGuid().ToString().Substring(0, 8)}.pdf";
        var filePath = Path.Combine(folderPath, fileName);

        var khachHang = lichTra.MaHopDongNavigation?.MaKhNavigation;
        var hopDong = lichTra.MaHopDongNavigation;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Calibri).FontSize(11));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Ngân hàng VAYTIEN").Bold().FontSize(16);
                        col.Item().Text("Địa chỉ: 123 Đường ABC, Quận 1, TP. HCM");
                        col.Item().Text("Website: www.vaytien.com");
                    });

                    row.ConstantItem(150).Text("HÓA ĐƠN THANH TOÁN").Bold().FontSize(18).FontColor("#0d6efd").AlignCenter();
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#dee2e6");
                    col.Spacing(20);

                    col.Item().Row(row =>
                    {
                        row.RelativeColumn().Column(column =>
                        {
                            column.Item().Text(txt =>
                            {
                                txt.Span("Mã hóa đơn: ").SemiBold();
                                txt.Span($"HD{lichTra.MaLich:D6}");
                            });
                            column.Item().Text(txt =>
                            {
                                txt.Span("Ngày thanh toán: ").SemiBold();
                                txt.Span($"{lichTra.NgayTra:dd/MM/yyyy}");
                            });
                        });

                        row.RelativeColumn().Column(column =>
                        {
                            column.Item().Text("THÔNG TIN KHÁCH HÀNG").Bold().FontColor("#6c757d");
                            column.Item().Text(khachHang?.HoTen);
                            column.Item().Text(khachHang?.Email);
                        });
                    });
                    col.Spacing(30);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#f8f9fa").Padding(5).Text("Nội dung thanh toán").Bold();
                            header.Cell().Background("#f8f9fa").Padding(5).Text("Số tiền").Bold().AlignRight();
                        });

                        table.Cell().Padding(5).Text($"Thanh toán cho Hợp đồng #{lichTra.MaHopDong}, Kỳ hạn #{lichTra.KyHanThu}");
                        table.Cell().Padding(5).Text($"{lichTra.SoTienPhaiTra:N0} VNĐ").AlignRight();

                        if (lichTra.SoTienPhat > 0)
                        {
                            table.Cell().Padding(5).Text("Phí phạt trả chậm");
                            table.Cell().Padding(5).Text($"+ {lichTra.SoTienPhat:N0} VNĐ").AlignRight().FontColor("#dc3545");
                        }

                        table.Cell().BorderTop(1).Padding(5).Text("TỔNG CỘNG").Bold().FontSize(12);
                        table.Cell().BorderTop(1).Padding(5).Text($"{((lichTra.SoTienPhaiTra ?? 0) + (lichTra.SoTienPhat ?? 0)):N0} VNĐ").AlignRight().Bold().FontSize(12);
                    });

                    col.Spacing(40);
                    col.Item().Text("Xác nhận thanh toán thành công. Cảm ơn Quý khách đã sử dụng dịch vụ!").AlignCenter();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(filePath);

        return filePath;
    }
}
