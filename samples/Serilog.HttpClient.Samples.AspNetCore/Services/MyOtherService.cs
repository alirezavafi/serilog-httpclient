// <copyright file="MyOtherService.cs" company="Sleep Outfitters USA">
// Copyright (c) Sleep Outfitters USA. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Serilog.HttpClient.Samples.AspNetCore.Services
{
    public class MyOtherService : IMyOtherService
    {
        private readonly System.Net.Http.HttpClient _httpClient;

        public MyOtherService(System.Net.Http.HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<object> SendRequest()
        {
            return _httpClient.GetFromJsonAsync<object>("https://jsonplaceholder.typicode.com/users/2");
        }
    }
}
