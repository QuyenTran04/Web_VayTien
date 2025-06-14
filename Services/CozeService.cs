using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace VAYTIEN.Services
{
    public class CozeService
    {
        private readonly string _token = "pat_Tgslgv2w5DLqZK1hofhj4azovBeeKexjZxa0JB0kbuxwFqyB4lPgD0KNFowf6gxl";
        private readonly string _botId = "7515655526622560257";

        public async Task<string> SendMessageAsync(string message)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var payload = new
            {
                bot_id = _botId,
                user = Guid.NewGuid().ToString(),
                query = message
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.coze.com/open_api/v2/chat", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }
    }
}
