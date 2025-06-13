using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class MoMoService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    public MoMoService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _http = httpClientFactory.CreateClient();
    }

    public async Task<string> CreatePaymentAsync(string orderId, long amount, string orderInfo, string returnUrl, string notifyUrl)
    {
        var momoConfig = _config.GetSection("MoMo");
        var endpoint = momoConfig["Endpoint"];
        var partnerCode = momoConfig["PartnerCode"];
        var accessKey = momoConfig["AccessKey"];
        var secretKey = momoConfig["SecretKey"];
        string requestId = orderId + DateTime.Now.Ticks;
        string extraData = "";

        string rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";
        string signature = HmacSHA256(rawHash, secretKey);

        var payload = new
        {
            partnerCode,
            accessKey,
            requestId,
            amount = amount.ToString(),
            orderId,
            orderInfo,
            redirectUrl = returnUrl,
            ipnUrl = notifyUrl,
            extraData,
            requestType = "captureWallet",
            signature,
            lang = "vi"
        };

        var json = JsonConvert.SerializeObject(payload);
        var res = await _http.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
        var resBody = await res.Content.ReadAsStringAsync();

        dynamic response = JsonConvert.DeserializeObject(resBody);
        if ((string)response.resultCode == "0")
        {
            return response.payUrl;
        }
        else
        {
            throw new Exception("Thanh toán thất bại: " + resBody);
        }
    }

    private string HmacSHA256(string message, string key)
    {
        var encoding = new UTF8Encoding();
        byte[] keyByte = encoding.GetBytes(key);
        byte[] messageBytes = encoding.GetBytes(message);
        using (var hmacsha256 = new HMACSHA256(keyByte))
        {
            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
            return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
        }
    }
}