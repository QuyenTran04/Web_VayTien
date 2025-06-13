using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService
{
    private readonly IConfiguration _config;
    public EmailService(IConfiguration config) => _config = config;

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var settings = _config.GetSection("EmailSettings");
        var client = new SmtpClient(settings["Host"], int.Parse(settings["Port"]))
        {
            Credentials = new NetworkCredential(settings["UserName"], settings["Password"]),
            EnableSsl = bool.Parse(settings["EnableSSL"])
        };

        var mail = new MailMessage(settings["UserName"], toEmail, subject, body) { IsBodyHtml = true };
        await client.SendMailAsync(mail);
    }
}
