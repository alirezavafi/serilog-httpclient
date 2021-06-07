using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Serilog.HttpClient.Samples.AspNetCore.Services
{
    public class MyService : IMyService
    {
        private readonly System.Net.Http.HttpClient _httpClient;

        public MyService(System.Net.Http.HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<object> SendRequest()
        {
            return _httpClient.GetFromJsonAsync<object>("https://reqres.in/api/users?page=2");
        }
    }
}