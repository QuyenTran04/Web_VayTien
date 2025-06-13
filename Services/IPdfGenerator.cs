using VAYTIEN.Models;

public interface IPdfGenerator
{
    string GeneratePaymentReceiptPdf(LichTraNo lichTra);
}
