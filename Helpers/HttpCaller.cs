using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FoosballApi.Helpers
{
    public class HttpCaller
    {
        public async Task<string> MakeApiCall(string bodyParam, string url)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("DatoCmsBearer"));
            HttpContent requestContent = new StringContent(bodyParam, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, requestContent);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<string> MakeApiCallSlack(string bodyParam, string url)
        {
            HttpClient client = new();

            HttpContent requestContent = new StringContent(bodyParam, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, requestContent);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }


        public async Task<string> CallSenderApi(string bodyParam, string url)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("resendApiKey"));
            HttpContent requestContent = new StringContent(bodyParam, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, requestContent);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
        
        public async Task<string> CallUnsendApi(string bodyParam, string url)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("unsendApiKey"));
            HttpContent requestContent = new StringContent(bodyParam, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, requestContent);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
    }
}
